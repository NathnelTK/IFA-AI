# Use Cases

## UC-01 Onboard learner
Input: learner conversation  
Output: validated LearnerProfile

## UC-02 Research learning goal
Input: LearnerProfile  
Output: ResearchPackage

## UC-03 Generate course blueprint
Input: LearnerProfile + ResearchPackage  
Output: CourseBlueprint

## UC-04 Generate current module
Input: CourseBlueprint + current module specification  
Output: persisted module/lesson content

## UC-05 Take assessment
Input: questions + learner answers  
Output: AssessmentAttempt + score + skill signals

## UC-06 Ask AI tutor
Input: learner question + course/module context + learner state  
Output: contextual response

## UC-07 Update learner profile
Input: explicit profile edit or AI-identified structured update  
Output: validated persisted profile change
