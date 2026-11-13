# Platform Improvements & Business Logic Updates

## 1. Account Access & Verification Flow

Current text:

there is text saying  Your account starts safely with learner access, then continues to educator verification before teaching tools are enabled. inside the authontication page both login, and create account

### Required Changes

- Educators must complete verification before accessing both learner or educator tools.
- After successful educator verification:
  - Redirect users directly to **Educator Studio**.
  - Do not redirect educators back to the learner homepage.
  - Educators and learners should have clearly separated experiences.
  - There should be no reason for educators to navigate through learner-specific pages.

### Verification Page

- Remove the sidebar from the educator verification page.
- The verification flow should feel focused and standalone.

---

## 2. Course Assistant Controls

### Current Problem

The Course Assistant is automatically embedded in every generated course.

### Required Changes

Not all educators want students to access the Course Assistant.

Add a course-level setting:

- ✅ Enable Course Assistant
- ✅ Disable Course Assistant

Educators should decide whether the assistant appears inside a course.

---

## 3. Course Overview & Description

### Current Problem

Courses lack descriptions and overviews.

Learners cannot easily determine:

- What the course covers
- Who the course is for
- What skills they will gain
- Whether the course matches their goals

### Required Changes

Every course must include:

- Course title
- Short description
- Detailed overview
- Learning outcomes
- Prerequisites
- Estimated duration
- Instructor information

### Enrollment Flow

Before enrollment, learners should see a dedicated:

# Course Description Page

This page should include:

- Course overview
- Module breakdown
- Learning objectives
- Assignments
- Quizzes
- Exams
- Scheduled live sessions
- Instructor profile
- have certificate or not

Only after reviewing this page should learners enroll.

---

## 4. Research Section Improvements

### Purpose

The Research section is intended to help learners explore concepts more deeply.

This is not meant to function like a chatbot that simply gives quick answers.

### Vision

Think of this feature as:

> "Google Scholar and academic research assistance for students."

Students should be able to:

- Discover research papers
- Find academic articles
- Access trusted educational resources
- Explore supporting materials
- Discover relevant tools
- Learn beyond course content

### Why It Matters

Many educational institutions are concerned about AI tools that only provide quick-generated answers.

Instead of replacing learning, this feature should:

- Encourage deeper exploration
- Surface credible sources
- Promote critical thinking
- Connect students with research-based content

### Research Results Should Include

- Research papers
- Scholarly articles
- Educational websites
- Books and references
- Public lectures
- Relevant YouTube educational resources
- Professional tools and frameworks

---

## 5. Settings Page Bug

### Current Issue

The Settings page displays:

> "Settings could not be loaded"

### Required Action

- Investigate and fix the settings-loading issue.
- Add proper error handling and fallback states.
- removeall unrelavent setting options and add some important options
---
## 6. Course Creation Workspace Bug

### Current Issue

The Workspace dropdown is not functioning.

### Required Action

- Fix Workspace selection.
- Ensure users can switch workspaces successfully before course creation.

---

# 7. Simplify Course Creation Methods

### Current Problem

Too many course creation options exist:

- Manual
- Prompt
- Material
- Template
- Voice Brief
 and didnot provide any choice for changeing canvas, color or font and styles
The workflow feels unnecessarily complex.

the solutions
have different types of canvas, color or font and styles that are reusable i have talk about it in detail at the end of the page

### Replace With Only Three Methods

## A. Manual Creation

Educators manually build courses.

Allow them to add:

- Text content
- Images
- YouTube URLs
- Downloadable files
- chose Templates
-chose styles like canvas and color, font , branding, and logo

The educator structures everything manually.

---

## B. Material-Based Creation

This becomes the primary workflow.

### Upload Process

Allow educators to bulk upload materials:

- PDFs
- DOCX files
- PPTs
- Images
- Notes
- Research papers
- Other educational resources
ask the model to build them the modules by it self or use module builder

### Module Builder

Provide:

- Module 1
- Module 2
- Module 3
- Add New Module

Each module should support multiple uploaded resources.

### AI Review & Validation

The AI should:

- Analyze uploaded content
- Detect duplicate information
- Detect conflicting information
- Suggest resolutions
- Ask the user for clarification when necessary

### Blueprint Generation

After validation:

1. User clicks Generate.
2. AI creates a course blueprint.
3. User reviews the blueprint.
4. User edits the structure.
5. User approves.
6. Course is generated.

### External Sources

Users should also be able to add:

- Websites
- Research articles
- YouTube videos
- External references

### Source Suggestions

The AI should be able to recommend:

- Additional research papers
- Relevant articles
- Extra references
- Educational videos

Educators can choose whether to include them.

### Note

Current "Source Research" and "Cover" sections are partially implementing these ideas.

Rather than separate sections, these capabilities should be integrated directly into:

- Material Creation
- AI Creation

---

## C. AI Course Creation

Designed for educators who:

- Have expertise
- Have ideas
- Do not have prepared course materials

### Workflow

The educator can chat with the AI about:

- Goals
- Learning outcomes
- Target audience
- Teaching plan
- External references
- YouTube channels
- Research papers
- Existing resources

The AI then:

1. Asks clarifying questions.
2. Creates a complete course blueprint.
3. Generates module structure.
4. Presents a preview.
5. Allows edits.
6. Publishes after approval.

---

# 8. Assessments & Learning Activities

Regardless of creation method, every course should support:

### Quizzes
- Auto-generated
- Manual creation
- AI-assisted creation

### Assignments
- Custom assignments
- Submission workflows

### Exams
- Timed assessments
- Question banks

### Video Sessions
- Schedule live classes
- Recurring sessions
- Calendar integration

These should be available for:

- Manual courses
- Material-generated courses
- AI-generated courses

---

# 9. Material Upload Experience

### Current UI Issues

The current PDF upload interface feels disconnected and limited.

### Improvements

Allow:

- Multiple file upload per module
- Drag-and-drop uploads
- Mixed content types

Examples:

Module 1:
- PDF
- PowerPoint
- Research paper
- YouTube video

Module 2:
- PDF
- Image collection
- External website

Module 3:
- DOCX
- Academic article

Users should be able to:

- Add sources
- Edit sources
- Remove sources
- Reorder sources

before generating the course.

---

# Final Product Vision

The platform should support two distinct user experiences:

### Learners
- Discover courses
- Review course descriptions
- Enroll confidently
- Access research tools for deeper learning
-comment in the courses
-perchase courses

### Educators
- Verify once
- Work entirely from Educator Studio
- Build courses manually, from materials, or with AI
- Control Course Assistant availability
- Generate structured learning experiences with quizzes, assignments, exams, and live sessions
-track learners progress

The overall goal is to make course creation simpler, smarter, and more flexible while giving educators full control over what learners can access.

## ui fix
- there are some sections that are not visible in darkmode like the complete session is not vissible in the home page of learners
-and check for other secctions that have the same problem
-some texts and codes are not vissible iin the generated courses in darkmode like bold and codes snips are not visible fix those
# Implement Canvas Studio – Reusable Course Designs, Themes, and Custom Fonts in Witforge

You are working on **Witforge**, an education platform that allows individual educators and educational institutions to create, publish, and manage courses.

I want to introduce a feature called **Canvas Studio**. It should allow educators to customize how their courses look and feel, save reusable designs, and eventually create their own layouts and upload custom fonts.

## 1. First, understand the existing codebase

Before making changes:

1. Inspect the existing frontend, backend, database models, API endpoints, and course rendering system.
2. Identify how courses, lessons, lesson content blocks, and published courses are currently stored and rendered.
3. Identify existing styling conventions, reusable components, CSS variables, and design tokens.
4. Identify how educators and institutions currently own or manage courses.
5. Check whether there are existing theme, template, or appearance-related features.
6. Identify any existing course data that could be affected.

**Do not immediately start creating new database tables or replacing existing components.** First understand the existing architecture and propose an implementation that fits it.

Use the existing stack and conventions:

- Frontend: SvelteKit 2, Svelte 5, Tailwind CSS.
- Backend: .NET 10 modular monolith.
- Database: PostgreSQL / Supabase.
- Existing deployment: Vercel frontend and Render backend.

Do not introduce another framework or unnecessary infrastructure.

## 2. Understand the three separate concepts

The implementation must distinguish between these concepts:

### A. Canvas (course layout)

A canvas determines the structural layout of the course learning experience.

Start with a few predefined layouts:

- **Classic:** Clean, traditional course layout.
- **Sidebar:** Lesson navigation on the left and content on the right.
- **Editorial:** Reading-focused layout with comfortable typography and wider content sections.
- **Presentation:** Large content area suitable for visual lessons and presentations.

These should be different rendering layouts, not just different colors.

### B. Theme (visual appearance)

A theme controls the visual appearance of the course:

- Primary color.
- Secondary color.
- Background color.
- Surface/card color.
- Text color.
- Heading font.
- Body font.
- Font sizes.
- Line height.
- Content width.
- Spacing.
- Border radius.
- Button styles.
- Card styles.
- Border styles.

Provide built-in color palettes and font combinations so educators can customize their courses without needing design experience.

### C. Design Preset (reusable design)

A design preset combines a canvas and theme into one reusable configuration.

For example:

**Academy Standard**
- Canvas: Editorial.
- Primary color: Forest green.
- Background: Cream.
- Cards: Rounded.
- Buttons: Solid.
- Heading font: Selected font.
- Body font: Selected font.

Educators should be able to:

- Save a design preset.
- Give it a name and description.
- Preview it.
- Apply it to a course.
- Reuse it in other courses they manage.
- Duplicate an existing preset.
- Edit their own presets.

The same design system should support individual educators and institutions according to their existing ownership and permission model.

## 3. Canvas Studio interface

Create an intuitive design editor for educators.

The interface should include:

- A live course preview.
- A canvas/layout selector.
- A color customization section.
- A typography section.
- A component styles section.
- A save-as-design-preset action.
- A preview action.
- An apply-to-course action.
- A reset-to-default action.

Suggested navigation tabs:

1. Canvas
2. Colors
3. Typography
4. Styles

The preview should update when the educator changes settings.

Keep the UI simple, professional, and consistent with Witforge's existing design system. Support both light and dark application themes where appropriate.

Avoid making it look like a complicated professional graphic-design application.

## 4. Custom canvas creation

Educators should eventually be able to create their own canvas layouts.

However, do not introduce an unrestricted website builder.

Use a controlled layout system based on reusable components and predefined layout regions.

For example, allow educators to customize:

- Sidebar visibility and position.
- Content width.
- Header placement.
- Lesson navigation placement.
- Content spacing.
- Optional course information sections.

Do not allow arbitrary JavaScript, arbitrary HTML, or unrestricted CSS uploads.

Start with the predefined layouts and build the architecture so additional custom layouts can be introduced safely.

If fully custom canvas creation is too large for the first implementation, keep it as a later phase rather than delaying the basic Canvas Studio feature.

## 5. Custom font uploads

Allow educators to upload fonts for use in their design presets.

Preferred support:

- WOFF2.
- WOFF.
- TTF as an optional format if safe and practical.

Requirements:

- Validate file extensions and actual file content.
- Set reasonable file-size limits.
- Store uploaded fonts using the existing storage solution if suitable.
- Associate fonts with the appropriate educator workspace or owner.
- Let educators select uploaded fonts for headings and body text.
- Preview the font before saving.
- Require educators to confirm they have the right to use the uploaded font.
- Prevent uploaded font files from being treated as executable content.
- Ensure only authorized workspace members can manage workspace fonts.

Use a safe font-loading mechanism such as controlled `@font-face` declarations.

Do not allow arbitrary CSS uploads alongside font uploads.

If the existing storage solution is insufficient, explain why before introducing another service.

## 6. Database and backend design

Inspect the existing data model before deciding what new entities are necessary.

Possible conceptual entities include:

- `WorkspaceDesign`: reusable design preset.
- `DesignVersion`: version of a saved design.
- `WorkspaceFont`: uploaded font metadata and storage reference.
- `CourseAppearance`: design configuration associated with a course.

These are suggestions, not mandatory table names. Reuse existing entities where appropriate and avoid unnecessary duplication.

A design configuration could conceptually look like this:

```json
{
  "canvas": {
    "layout": "editorial",
    "contentWidth": "760px",
    "spacing": "comfortable"
  },
  "colors": {
    "primary": "#245B45",
    "background": "#F7F5EF",
    "surface": "#FFFFFF",
    "text": "#252525"
  },
  "typography": {
    "headingFontId": "font-001",
    "bodyFontId": "font-002"
  },
  "components": {
    "cards": "rounded",
    "buttons": "solid",
    "borders": "subtle"
  }
}
```

This is an example configuration, not a requirement to copy directly.

Backend requirements:

- Validate all design configuration values.
- Validate font ownership and permissions.
- Ensure educators cannot access another workspace's private designs or fonts.
- Use the existing authentication and authorization system.
- Keep API contracts consistent with existing conventions.
- Avoid exposing private storage paths or credentials.

## 7. Most important: do not break existing courses

This is a critical requirement.

Existing courses must continue working exactly as they do today.

Follow these rules:

1. Do not rewrite existing course content to introduce Canvas Studio.
2. Do not change lesson IDs, course IDs, enrollment records, learner progress, payments, or existing course URLs.
3. Existing courses without an appearance configuration must automatically use the current Witforge design.
4. Adding or changing a design must not modify lesson content.
5. Design changes must not affect learner progress or course completion records.
6. Do not force existing courses to adopt a new canvas.
7. Do not silently change the appearance of already-published courses.

### Design versioning

A published course should use a stable design version.

If an educator edits a design preset later, do not automatically change every published course using it.

Instead, provide an explicit option to:

- Keep the current published design.
- Apply the new version to selected courses.
- Apply the new version to all courses using that preset.

Preserve old design versions while published courses still depend on them.

If a font is used by a published course, prevent accidental deletion from breaking that course. Use safe deletion rules or retain the required font asset until it is no longer referenced.

## 8. Frontend rendering architecture

Create a shared rendering system for course appearance.

The general principle:

**Course content is separate from course presentation.**

Course content includes:

- Headings.
- Paragraphs.
- Images.
- Videos.
- Quizzes.
- Assignments.
- Other lesson content blocks.

Course presentation includes:

- Canvas layout.
- Colors.
- Fonts.
- Spacing.
- Cards.
- Buttons.
- Navigation appearance.

The rendering system should take the course content and selected appearance configuration and render them together.

Use CSS variables or another controlled design-token approach for dynamic values.

For example:

```css
.course-theme {
  --course-primary: #245B45;
  --course-background: #F7F5EF;
  --course-surface: #FFFFFF;
  --course-text: #252525;
  --course-radius: 12px;
  --course-content-width: 760px;
}
```

Do not construct arbitrary Tailwind class names dynamically from database values. Tailwind may not generate those classes at build time.

Use validated CSS variables, shared components, and predefined layout variants instead.

## 9. Implementation phases

Implement this incrementally.

### Phase 0 – Audit and protection

- Audit the existing course rendering architecture.
- Identify all affected components and models.
- Document the existing default appearance.
- Identify migration and backward-compatibility risks.
- Propose the smallest safe implementation.

Do not make destructive changes.

### Phase 1 – Basic Canvas Studio

- Extract existing course styles into reusable design tokens.
- Keep the current design as the default.
- Introduce the four predefined canvas layouts.
- Add built-in color palettes.
- Add built-in font combinations.
- Add controls for basic colors, typography, spacing, cards, and buttons.
- Implement a live preview.

At this stage, existing courses should remain unchanged unless an educator explicitly applies a new design.

### Phase 2 – Reusable design presets

- Allow educators to save custom design presets.
- Allow duplication, editing, and deletion.
- Allow applying presets to multiple courses.
- Add design versioning.
- Add safe publishing and update controls.

### Phase 3 – Custom fonts

- Implement secure font upload.
- Validate font files.
- Store font metadata and files.
- Add font selection and preview.
- Ensure published courses retain access to fonts they use.

### Phase 4 – Advanced canvas customization

Only after the previous phases are stable:

- Allow controlled custom layout creation.
- Allow additional layout regions and reusable sections.
- Allow educators to save their own canvas templates.
- Make custom canvases reusable across their courses.

Do not implement every phase at once if doing so creates unnecessary risk.

## 10. Testing and acceptance criteria

Before considering the implementation complete, verify:

- Existing courses still render correctly without a design configuration.
- Existing learners can continue their courses.
- Existing enrollments and progress remain intact.
- Changing a theme does not modify course content.
- Changing a canvas does not modify lesson records.
- A saved design can be reused across multiple courses.
- Different educators cannot access each other's private designs or fonts.
- Workspace members can only perform actions allowed by their roles.
- Uploaded fonts load correctly.
- Invalid or oversized font files are rejected.
- Published courses retain their selected design version.
- Editing a reusable preset does not silently change published courses.
- The editor works on desktop and mobile.
- Both light and dark application themes remain usable.

Add appropriate backend tests, frontend tests, and migration tests based on the existing testing setup.

## 11. Development rules

- Work with the existing codebase rather than rebuilding it.
- Keep the backend as a modular monolith.
- Avoid introducing unnecessary packages or infrastructure.
- Do not make unrelated changes to authentication, payments, enrollment, or course creation.
- Do not remove existing functionality.
- Use backward-compatible database migrations.
- Never delete or overwrite existing production data.
- Keep changes small and reviewable.
- Follow existing naming and coding conventions.
- Do not use fake data in production functionality.
- Do not claim a feature is complete if it only has a UI mockup.

## 12. How I want you to work

Do not implement everything blindly in one step.

First, audit the repository and return:

1. How courses and lessons currently work.
2. How the current course appearance is implemented.
3. Which existing components can be reused.
4. Which database changes are actually necessary.
5. Potential risks to existing courses and learner data.
6. A recommended implementation sequence.
7. Any assumptions or missing information.

Then begin Phase 0 and Phase 1.

After Phase 1 is implemented, run the available tests and report:

- Files changed.
- Database migrations created.
- New functionality.
- Tests performed and results.
- Any remaining issues.
- Whether existing courses remain backward-compatible.


**Final priority:** Canvas Studio should make Witforge courses more customizable and professional without turning the platform into a complicated website builder or risking existing course data. Keep the implementation maintainable, affordable, and suitable for a small development team.
