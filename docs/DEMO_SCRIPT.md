# IFA — Demo Script

A single end-to-end use case for presenting IFA.

**Runtime:** ~3:10
**Live:** https://ifa-ai-red.vercel.app · **API:** https://ifa-api-8kgf.onrender.com

**The use case:** Negede, a third-year software engineering student, sits his exit exam in three
weeks. He needs backend API fundamentals in C#, and he already has a lecture PDF and a GitHub repo he
wants the course to respect.

---

## Pre-flight (before you present)

1. **Open the app two minutes early and click "Sign in".** The API runs on Render's free tier and
   sleeps after ~15 minutes idle; the first request takes about a minute to wake. Do not let a cold
   start eat your opening line.
2. **Sign in as the demo learner** — `nathnel@ifa.local` / `ifa12345`. Skip registration on stage.
3. **Set dark mode** via the toggle in the header.
4. If your slot is under three minutes, fill the chat and setup form **up to the Approve button**
   beforehand, then re-run live from Approve.

---

## Scene 1 — The hook (0:00 – 0:20)

**[SCREEN: Home. The hero chat is idle, wave background animating.]**

> Every learning platform starts by asking you to pick a course from a catalog someone else wrote.
> IFA starts by asking what you're actually trying to achieve — and then builds the course around
> that answer. Let me show you one student's whole journey in three minutes.

## Scene 2 — Model 1: the conversation (0:20 – 0:50)

**[SCREEN: Type into the hero chat.]**

**[TYPE]** `I'm preparing for my exit exam and I need C# backend API fundamentals`

> This is our first model — the learning advisor. It's not a search box. Watch what it does with that
> sentence.

**[SAY]** `About 6 hours a week`
**[SAY]** `I prefer short practical walkthroughs`

> It pulls the goal, the weekly hours and the creator preference into a learner profile. And note what
> it *doesn't* do: it won't build a course out of "hi" or a one-word answer. It keeps asking until it
> genuinely understands the goal — that guard is deliberate, and it's the difference between a course
> that fits and one that's generic.

## Scene 3 — The setup form (0:50 – 1:15)

**[SCREEN: The course setup form appears, pre-filled from the conversation.]**

> Everything it learned is now pre-filled. This is also where you hand IFA your own material.

**[SCREEN: Paste into the materials field.]**

`https://github.com/NathnelTK/IFA-AI` (or his lecture PDF link)

> That link matters later. Every lesson in this course has to consider it — and I'll show you that it
> actually shows up in the content, not just in the prompt. Cover image is optional; if you skip it,
> IFA generates one from the course initials.

## Scene 4 — Model 2: the blueprint (1:15 – 1:40)

**[SCREEN: Click Generate. The blueprint appears — modules, topics, hours.]**

> This is model two, the course architect. This is the checkpoint that matters: you get a reviewable
> plan — modules, the topics inside them, hour estimates — *before* a single lesson is written. You can
> change any of it. Nothing has been generated yet.

## Scene 5 — Approve: research and the first module (1:40 – 2:05)

**[SCREEN: Click Approve & generate. Loading state briefly, then Module 1 marked Ready.]**

> The moment you approve, three things happen. Research runs: peer-reviewed papers through
> ScholarXiv, plus web references and YouTube walkthroughs matched to the creator he asked for. That
> research is saved with the course and reused, so later modules don't pay for it twice. Second, the
> cover image is resolved. And third, module one is written immediately — four lessons and exam-style
> quizzes — and you're auto-enrolled.

**[SCREEN: Scroll Module 1, open a lesson, scroll to the bottom.]**

> Here's the part I want you to notice. Scroll to the end of the lesson: **Course Sources**. Our own
> GitHub link is cited in there alongside the researched material. The model is told to use it, and we
> don't rely on it complying — it's enforced.

## Scene 6 — Learning, and the adaptive loop (2:05 – 2:30)

**[SCREEN: Open the lesson, then take a quiz and miss a couple of questions.]**

> He reads the lesson and takes the quiz. This is where the adaptive engine earns its name.

**[SCREEN: Skills / Progress page — weak areas flagged.]**

> Miss questions on a topic and it shows up here as a weak area — and it feeds straight back into what
> gets recommended and what the next module emphasises. The course reshapes itself around what he's
> actually getting wrong.

## Scene 7 — Voxide voice, and just-in-time modules (2:30 – 2:50)

**[SCREEN: Click the mic. Speak.]**

**[SAY]** `Create a course on data structures`

> This is Voxide. Voice isn't a gimmick bolted on top — a spoken request maps onto the same
> capabilities the UI calls, so it runs the real flow.

**[SCREEN: Navigate back to the course. Click Generate on Module 2.]**

> And module two? It doesn't exist yet. He generates it on demand when he reaches it, reusing the
> research from earlier. Modules materialise as he needs them, not all at once.

## Scene 8 — Close (2:50 – 3:10)

**[SCREEN: Back to Home, then the stack.]**

> That's one student, one goal, zero course catalogues. Three models with three separate jobs — a
> conversational advisor, an architect you approve, and a builder that writes lessons grounded in real
> research. SvelteKit and a .NET clean-architecture API, deployed on Vercel and Render, running right
> now. IFA: you describe the outcome, it builds the path. Thank you.

---

## If a judge asks

- **"Is the AI real or mocked?"** — Real. Gemini for conversation, Groq for fast inference with
  automatic failover, ScholarXiv for research. The API exposes it: hit `/api/health` and you'll see
  all four pipelines listed with the database live.
- **"What makes this different from ChatGPT writing a syllabus?"** — The approval gate and the research
  reuse. You inspect a blueprint before content is generated, and sources are enforced into the lesson
  rather than requested politely.
- **"What's next?"** — Mobile, richer analytics, and instructor tooling — the marketplace and
  courses-created-by-AI already exist as the foundation.

## 60-second cut

Run **Scene 2 → Scene 5 → Scene 8** and swap the punchline:

> Chat, approve a blueprint, get a research-backed module with your own sources cited. Three models,
> one conversation.
