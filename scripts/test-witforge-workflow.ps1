param(
    [Parameter(Mandatory = $true)]
    [string]$ApiBaseUrl,
    [switch]$TestAi
)

$ErrorActionPreference = 'Stop'
$ApiBaseUrl = $ApiBaseUrl.TrimEnd('/')

function Invoke-JsonRequest {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body,
        [string]$Token,
        [int[]]$ExpectedStatus = @(200)
    )

    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $parameters = @{
        Uri = "$ApiBaseUrl$Path"
        Method = $Method
        Headers = $headers
        UseBasicParsing = $true
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 20 -Compress
    }

    try {
        $response = Invoke-WebRequest @parameters
        $status = [int]$response.StatusCode
        $content = $response.Content
    }
    catch {
        if ($null -eq $_.Exception.Response) { throw }
        $response = $_.Exception.Response
        $status = [int]$response.StatusCode
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        try { $content = $reader.ReadToEnd() } finally { $reader.Dispose() }
    }

    if ($ExpectedStatus -notcontains $status) {
        throw "$Method $Path returned $status; expected $($ExpectedStatus -join ','). Body: $content"
    }
    $parsed = if ([string]::IsNullOrWhiteSpace($content)) { $null } else { $content | ConvertFrom-Json }
    return [pscustomobject]@{ Status = $status; Body = $parsed }
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

$suffix = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$educatorEmail = "educator-$suffix@witforge.test"
$learnerEmail = "learner-$suffix@witforge.test"

$anonymousCreate = Invoke-JsonRequest POST '/api/educator/courses/manual' @{
    title = 'Forbidden anonymous course'
} $null @(401)
Assert-True ($anonymousCreate.Status -eq 401) 'anonymous users must not create courses'

$educator = (Invoke-JsonRequest POST '/api/auth/register' @{
    name = 'Integration Educator'; email = $educatorEmail; password = 'Integration!123'; role = 'Educator'
} $null).Body
$learner = (Invoke-JsonRequest POST '/api/auth/register' @{
    name = 'Integration Learner'; email = $learnerEmail; password = 'Integration!123'; role = 'Learner'
} $null).Body
$relogin = (Invoke-JsonRequest POST '/api/auth/login' @{
    email = $learnerEmail; password = 'Integration!123'
} $null).Body
$badLogin = Invoke-JsonRequest POST '/api/auth/login' @{
    email = $learnerEmail; password = 'wrong-password'
} $null @(401)
Assert-True ($relogin.learnerId -eq $learner.learnerId -and $badLogin.Status -eq 401) 'versioned password hashes must support valid login and reject invalid credentials'

$checkCount = 25
if ($TestAi) {
    $aiResult = Invoke-JsonRequest POST '/api/ai/course/propose' @{
        goal = 'A provider integration check'; hoursPerWeek = 2; preferredCreator = 'None'
    } $educator.accessToken @(200, 503)
    if ($aiResult.Status -eq 503) {
        Assert-True ($aiResult.Body.code -eq 'ai_unavailable') 'missing AI providers must fail honestly instead of fabricating a course'
    }
    else {
        Assert-True (-not [string]::IsNullOrWhiteSpace([string]$aiResult.Body.courseTitle) -and $aiResult.Body.modules.Count -gt 0) 'a configured provider must return a usable blueprint'
    }
    $checkCount++
}

$created = (Invoke-JsonRequest POST '/api/educator/courses/manual' @{
    title = 'Persisted Witforge Workflow'
    description = 'A real persisted workflow used by the isolated integration check.'
    category = 'Software Engineering'
    targetAudience = 'Developers validating course delivery'
    estimatedDuration = '1 week'
    level = 'Beginner'
    language = 'English'
    visibility = 'Public'
    enrollmentMode = 'Open'
    learningObjectives = @('Explain the workflow.', 'Complete a persisted lesson and assessment.')
    requirePassingQuizzes = $true
    modules = @(@{
        title = 'Workflow foundations'
        summary = 'The first module.'
        estimatedHours = 1
        lessons = @(@{
            title = 'Persisted lesson'
            summary = 'A saved lesson.'
            contentMarkdown = '# Persisted lesson`n`nThis content comes from PostgreSQL.'
            readingTimeMinutes = 5
        })
    })
} $educator.accessToken).Body

$draftDenied = Invoke-JsonRequest GET "/api/courses/$($created.courseId)" $null $learner.accessToken @(403)
$draftEnrollDenied = Invoke-JsonRequest POST "/api/courses/$($created.courseId)/enroll" $null $learner.accessToken @(409)
Assert-True ($draftDenied.Status -eq 403 -and $draftEnrollDenied.Status -eq 409) 'draft content and enrollment must be denied'
$privateSearch = (Invoke-JsonRequest GET '/api/search?q=Persisted%20Witforge%20Workflow' $null $learner.accessToken).Body
Assert-True (@($privateSearch.courses | Where-Object { $_.id -eq $created.courseId }).Count -eq 0 -and @($privateSearch.lessons | Where-Object { $_.courseId -eq $created.courseId }).Count -eq 0) 'unpublished course metadata must not leak through global search'

$ownerDraft = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)" $null $educator.accessToken).Body
Assert-True ($ownerDraft.isOwner -and $ownerDraft.hasContentAccess) 'owner preview must expose saved draft content'
$moduleId = [string]$ownerDraft.modules[0].id
$lessonId = [string]$ownerDraft.modules[0].lessons[0].id

$quizSave = (Invoke-JsonRequest PUT "/api/educator/courses/$($created.courseId)/modules/$moduleId/quizzes" @{
    quizzes = @(@{
        title = 'Workflow check'
        passingScorePercentage = 70
        kind = 0
        questions = @(@{
            prompt = 'Where is learner progress persisted?'
            options = @('Browser memory only', 'PostgreSQL through the API')
            correctOptionIndex = 1
            explanation = 'The API stores lesson and assessment evidence.'
            targetSkillName = 'Course workflow'
        })
    })
} $educator.accessToken).Body
Assert-True ($quizSave.quizzes.Count -eq 1) 'the educator must be able to author an assessment'

$assignment = (Invoke-JsonRequest POST "/api/educator/courses/$($created.courseId)/assignments" @{
    title = 'Workflow reflection'
    instructionsMarkdown = 'Explain how authorization and persistence work in this course.'
    dueDaysAfterEnrollment = 7
    rubricCriteriaJson = '[]'
    maxScore = 100
} $educator.accessToken).Body
$session = (Invoke-JsonRequest POST "/api/educator/courses/$($created.courseId)/live-sessions" @{
    title = 'Workflow live review'
    description = 'Review the persisted course workflow.'
    startTime = [DateTimeOffset]::UtcNow.AddDays(1).ToString('o')
    durationMinutes = 45
    timeZone = 'UTC'
    meetingUrl = 'https://meet.example.test/witforge-review'
} $educator.accessToken).Body
Assert-True ($assignment.submissionMode -eq 'TextOnly' -and $session.status -eq 'Scheduled') 'text assignments and external-link live sessions must persist'

$published = (Invoke-JsonRequest POST "/api/educator/courses/$($created.courseId)/publish" @{
    isPaid = $false
} $educator.accessToken).Body
Assert-True ($published.isPublished -and $published.isPublic) 'the validated course must publish'

$publicMetadata = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)" $null).Body
Assert-True (-not $publicMetadata.hasContentAccess) 'anonymous metadata must not grant content access'
Assert-True ([string]::IsNullOrEmpty([string]$publicMetadata.modules[0].lessons[0].contentMarkdown)) 'anonymous lesson content must be redacted'
Assert-True ($publicMetadata.modules[0].quizzes.Count -eq 0) 'anonymous assessments must be redacted'

$anonymousShare = Invoke-JsonRequest POST '/api/CourseSharing/share' @{ courseId = $created.courseId } $null @(401)
$share = (Invoke-JsonRequest POST '/api/CourseSharing/share' @{
    courseId = $created.courseId
    sharedByLearnerId = $learner.learnerId
} $educator.accessToken).Body
$sharedEnrollment = (Invoke-JsonRequest POST "/api/CourseSharing/enroll/$($share.shareCode)" $null $learner.accessToken).Body
Assert-True ($anonymousShare.Status -eq 401 -and $sharedEnrollment.status -eq 'Active') 'share routes must authenticate and derive both actors from JWT identity'
$enrollment = (Invoke-JsonRequest POST "/api/courses/$($created.courseId)/enroll" $null $learner.accessToken).Body
Assert-True ($enrollment.status -eq 'Active' -and -not $enrollment.paymentRequired) 'open free enrollment must become active'

$activeCourse = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)" $null $learner.accessToken).Body
$quiz = $activeCourse.modules[0].quizzes[0]
$question = $quiz.questions[0]
Assert-True ($activeCourse.hasContentAccess) 'active enrollment must grant content access'
Assert-True (-not [string]::IsNullOrWhiteSpace([string]$activeCourse.modules[0].lessons[0].contentMarkdown)) 'active learner must receive lesson content'
Assert-True ($question.PSObject.Properties.Name -notcontains 'correctOptionIndex') 'answer keys must not be returned before submission'

$firstCompletion = (Invoke-JsonRequest POST "/api/lessons/$lessonId/complete" $null $learner.accessToken).Body
$secondCompletion = (Invoke-JsonRequest POST "/api/lessons/$lessonId/complete" $null $learner.accessToken).Body
Assert-True ($firstCompletion.progressPercentage -eq 50) 'one lesson plus one required quiz should yield 50 percent after the lesson'
Assert-True ($secondCompletion.alreadyCompleted -and $secondCompletion.progressPercentage -eq 50) 'lesson completion must be idempotent'
$me = (Invoke-JsonRequest GET '/api/auth/me' $null $learner.accessToken).Body
Assert-True ($me.state.completedLessonsCount -eq 1) 'repeat completion must not inflate learner totals'

$partial = Invoke-JsonRequest POST "/api/quizzes/$($quiz.id)/submit" @{ answers = @{} } $learner.accessToken @(400)
Assert-True ($partial.Status -eq 400) 'partial quiz answers must be rejected'
$correctAnswers = @{}; $correctAnswers[[string]$question.id] = 1
$correct = (Invoke-JsonRequest POST "/api/quizzes/$($quiz.id)/submit" @{ answers = $correctAnswers } $learner.accessToken).Body
Assert-True ($correct.scorePercentage -eq 100 -and $correct.isPassed -and $correct.progressPercentage -eq 100) 'server grading must complete the course'
Assert-True ($correct.questionFeedbacks[0].correctOptionIndex -eq 1) 'answer feedback may reveal the key only after submission'

$wrongAnswers = @{}; $wrongAnswers[[string]$question.id] = 0
$null = Invoke-JsonRequest POST "/api/quizzes/$($quiz.id)/submit" @{ answers = $wrongAnswers } $learner.accessToken
$third = (Invoke-JsonRequest POST "/api/quizzes/$($quiz.id)/submit" @{ answers = $wrongAnswers } $learner.accessToken).Body
$attemptLimit = Invoke-JsonRequest POST "/api/quizzes/$($quiz.id)/submit" @{ answers = $correctAnswers } $learner.accessToken @(409)
Assert-True ($third.attemptCount -eq 3 -and $attemptLimit.Status -eq 409) 'quiz attempt limits must be enforced'

$coldCourse = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)" $null $learner.accessToken).Body
Assert-True ($coldCourse.progressPercentage -eq 100 -and $coldCourse.modules[0].lessons[0].isCompleted) 'persisted progress must survive a cold read'
Assert-True ($coldCourse.modules[0].quizzes[0].attemptCount -eq 3) 'persisted attempts must survive a cold read'
$ownerCourse = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)" $null $educator.accessToken).Body
Assert-True ($ownerCourse.modules[0].quizzes[0].questions[0].correctOptionIndex -eq 1) 'the owner editor must receive the saved answer key'

$assignments = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)/assignments" $null $learner.accessToken).Body
$submitted = (Invoke-JsonRequest POST "/api/courses/$($created.courseId)/assignments/$($assignment.id)/submit" @{
    submissionText = 'Authorization is enforced on the API and progress is persisted in PostgreSQL.'
} $learner.accessToken).Body
$submissions = (Invoke-JsonRequest GET "/api/educator/courses/$($created.courseId)/assignments/$($assignment.id)/submissions" $null $educator.accessToken).Body
$graded = (Invoke-JsonRequest POST "/api/educator/assignments/submissions/$($submitted.id)/grade" @{
    score = 92; feedback = 'Clear evidence from the working flow.'
} $educator.accessToken).Body
$gradedAssignments = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)/assignments" $null $learner.accessToken).Body
Assert-True ($assignments.Count -eq 1 -and $submissions.Count -eq 1 -and $graded.score -eq 92 -and $gradedAssignments[0].mySubmission.feedback) 'assignment submission, grading, and feedback must persist'
$unauthorizedResubmit = Invoke-JsonRequest POST "/api/courses/$($created.courseId)/assignments/$($assignment.id)/submit" @{
    submissionText = 'This must not erase the completed grade.'
} $learner.accessToken @(409)
Assert-True ($unauthorizedResubmit.Status -eq 409) 'a graded assignment must require an explicit instructor resubmission request'

$learnerSessions = (Invoke-JsonRequest GET "/api/courses/$($created.courseId)/live-sessions" $null $learner.accessToken).Body
$updatedSession = (Invoke-JsonRequest PUT "/api/educator/courses/$($created.courseId)/live-sessions/$($session.id)" @{
    title = 'Updated workflow live review'
    description = 'The schedule changed.'
    startTime = [DateTimeOffset]::UtcNow.AddDays(2).ToString('o')
    durationMinutes = 60
    timeZone = 'UTC'
    meetingUrl = 'https://meet.example.test/witforge-review-updated'
} $educator.accessToken).Body
$cancelledSession = (Invoke-JsonRequest DELETE "/api/educator/courses/$($created.courseId)/live-sessions/$($session.id)" $null $educator.accessToken).Body
$notifications = (Invoke-JsonRequest GET '/api/notifications' $null $learner.accessToken).Body
Assert-True ($learnerSessions.Count -eq 1 -and $updatedSession.status -eq 'Scheduled' -and $cancelledSession.status -eq 'Cancelled') 'live sessions must be visible, editable, and cancellable'
Assert-True (@($notifications | Where-Object { $_.type -like 'live_session_*' }).Count -eq 2) 'schedule changes must notify active learners'

$publishedEdit = Invoke-JsonRequest PUT "/api/educator/courses/$($created.courseId)/branding" @{ title = 'Unsafe live edit' } $educator.accessToken @(409)
Assert-True ($publishedEdit.Status -eq 409) 'published content must require unpublishing before edits'
$null = Invoke-JsonRequest POST "/api/educator/courses/$($created.courseId)/unpublish" $null $educator.accessToken
$draftLearner = Invoke-JsonRequest GET "/api/courses/$($created.courseId)" $null $learner.accessToken @(403)
Assert-True ($draftLearner.Status -eq 403) 'active enrollments must not expose an unpublished draft'
$paidWithoutProvider = Invoke-JsonRequest POST "/api/educator/courses/$($created.courseId)/publish" @{
    isPaid = $true; price = 100; currency = 'ETB'
} $educator.accessToken @(409)
Assert-True ($paidWithoutProvider.Status -eq 409) 'paid publication must not simulate checkout without configured payment credentials'
$null = Invoke-JsonRequest POST "/api/educator/courses/$($created.courseId)/publish" @{ isPaid = $false } $educator.accessToken

$learnerDashboard = Invoke-JsonRequest GET '/api/educator/dashboard' $null $learner.accessToken @(403)
$learnerTemplates = Invoke-JsonRequest GET '/api/educator/templates' $null $learner.accessToken @(403)
Assert-True ($learnerDashboard.Status -eq 403 -and $learnerTemplates.Status -eq 403) 'learner role must not access educator data'
$crossAccountSkills = Invoke-JsonRequest GET "/api/skills/$($educator.learnerId)" $null $learner.accessToken @(403)
$crossAccountProfile = Invoke-JsonRequest GET "/api/learners/$($educator.learnerId)/profile/latest" $null $learner.accessToken @(403, 404)
Assert-True ($crossAccountSkills.Status -eq 403 -and $crossAccountProfile.Status -in @(403, 404)) 'learner profile and skill routes must enforce self access'
$dashboard = (Invoke-JsonRequest GET '/api/educator/dashboard' $null $educator.accessToken).Body
$metric = @($dashboard.courses | Where-Object { $_.courseId -eq $created.courseId })[0]
Assert-True ($metric.completedEnrollments -eq 1 -and $metric.completionRate -eq 100) 'educator completion metrics must use persisted learner evidence'
Assert-True ($metric.assessmentAttempts -eq 3 -and $metric.avgAssessmentScore -eq 33) 'educator assessment metrics must reflect real attempts'
Assert-True ($metric.assignmentSubmissions -eq 1 -and $metric.pendingGrading -eq 0) 'assignment metrics must reflect persisted grading status'

Write-Output ([pscustomobject]@{
    status = 'passed'
    courseId = $created.courseId
    projectId = $created.projectId
    finalProgress = $coldCourse.progressPercentage
    assessmentAttempts = $metric.assessmentAttempts
    checks = $checkCount + 12
} | ConvertTo-Json -Compress)
