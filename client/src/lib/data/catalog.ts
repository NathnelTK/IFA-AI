import type { Course } from '$lib/types';

/**
 * Demo course catalog — the single source of truth for the interactive demo.
 *
 * Every page (home, courses, my-learning, course detail, quiz) reads from the
 * courses store which is seeded from this data, so completing a lesson or quiz
 * anywhere is reflected everywhere. Replace with real API data once the AI
 * pipeline lands.
 */
export const demoCourses: Course[] = [
  {
    id: 'csharp-backend',
    title: 'C# Backend Development',
    provider: 'IFA Generated',
    description:
      'Master backend development with C# and ASP.NET Core — from language fundamentals to building and securing production REST APIs.',
    thumbnail: 'C#',
    category: 'Backend',
    duration: '6 weeks',
    status: 'inProgress',
    isBookmarked: true,
    enrolledDate: '2 weeks ago',
    lastAccessed: '2 hours ago',
    modules: [
      {
        id: 'csharp-m1',
        title: 'C# Language Fundamentals',
        summary: 'Types, control flow, and the building blocks of C#.',
        lessons: [
          {
            id: 'csharp-m1-l1',
            title: 'Variables & Types',
            summary: 'Value vs reference types, and how the CLR stores them.',
            duration: '8 min',
            completed: true,
            content: [
              'C# is a statically typed language: every variable has a type known at compile time. Types fall into two families — value types (int, bool, structs) live on the stack or inline, while reference types (class instances, strings, arrays) live on the heap and are accessed through a reference.',
              'Use `var` when the type is obvious from the right-hand side, and an explicit type when it aids readability. Prefer `int` for whole numbers, `decimal` for money, and `double` for scientific values.',
              'Nullable reference types (enabled by default in modern C#) help the compiler catch null-dereference bugs before they reach production.'
            ]
          },
          {
            id: 'csharp-m1-l2',
            title: 'Control Flow',
            summary: 'if/switch expressions, loops, and pattern matching.',
            duration: '10 min',
            completed: true,
            content: [
              'Modern C# favors expressions over statements. A switch expression returns a value and is exhaustive-checked by the compiler, which makes it far safer than a long if/else chain.',
              'Pattern matching lets you branch on the shape of data: type patterns, property patterns, and relational patterns all compose inside a single switch.',
              'For iteration, prefer foreach over index-based for loops unless you need the index — it reads more clearly and works over any IEnumerable.'
            ]
          },
          {
            id: 'csharp-m1-l3',
            title: 'Methods & Classes',
            summary: 'Encapsulation, properties, and constructors.',
            duration: '12 min',
            completed: false,
            content: [
              'Classes bundle data (fields) with behavior (methods). Expose state through properties rather than public fields so you can add validation later without breaking callers.',
              'Constructors initialize an object into a valid state. Use primary constructors for concise records and dependency injection.',
              'Keep methods small and single-purpose — a method that does one thing is easier to name, test, and reuse.'
            ]
          }
        ],
        quizzes: [{
          isExam: true,
          orderIndex: 1,
          id: 'csharp-m1-quiz',
          title: 'Fundamentals Check',
          bestScore: null,
          questions: [
            {
              id: 'q1',
              prompt: 'Which of these is a value type in C#?',
              options: ['string', 'int', 'object', 'array'],
              correctIndex: 1,
              explanation: '`int` is a value type stored inline; string, object, and array are reference types.'
            },
            {
              id: 'q2',
              prompt: 'What is the main advantage of a switch expression over an if/else chain?',
              options: [
                'It runs faster at any size',
                'It returns a value and is exhaustiveness-checked',
                'It supports more than 10 branches',
                'It works only with integers'
              ],
              correctIndex: 1,
              explanation: 'Switch expressions return a value and let the compiler verify all cases are handled.'
            },
            {
              id: 'q3',
              prompt: 'Why expose class state through properties instead of public fields?',
              options: [
                'Properties are always faster',
                'Fields cannot be serialized',
                'Properties let you add validation later without breaking callers',
                'C# forbids public fields'
              ],
              correctIndex: 2,
              explanation: 'Properties preserve the API surface, so you can add logic later without a breaking change.'
            }
          ]
        }]
      },
      {
        id: 'csharp-m2',
        title: 'Working with Databases',
        summary: 'EF Core, LINQ queries, and data modeling.',
        lessons: [
          {
            id: 'csharp-m2-l1',
            title: 'Entity Framework Core Basics',
            summary: 'DbContext, entities, and migrations.',
            duration: '14 min',
            completed: true,
            content: [
              'EF Core is an object-relational mapper: you model your data as C# classes and EF translates LINQ queries into SQL.',
              'A DbContext represents a session with the database and exposes DbSet<T> properties for each entity. Migrations version your schema so it evolves alongside your code.',
              'Prefer async methods (ToListAsync, SaveChangesAsync) to keep request threads free under load.'
            ]
          },
          {
            id: 'csharp-m2-l2',
            title: 'LINQ Queries',
            summary: 'Filtering, projection, and joins.',
            duration: '11 min',
            completed: false,
            content: [
              'LINQ gives you a single query syntax across in-memory collections and databases. Where filters, Select projects, and Include eager-loads related data.',
              'Be mindful of deferred execution: a query does not run until you enumerate it. Materialize with ToList/ToListAsync when you need the results more than once.',
              'Push filtering to the database (IQueryable) rather than pulling everything into memory and filtering client-side.'
            ]
          }
        ],
        quizzes: [{
          isExam: true,
          orderIndex: 1,
          id: 'csharp-m2-quiz',
          title: 'Databases Check',
          bestScore: null,
          questions: [
            {
              id: 'q1',
              prompt: 'What does a DbContext represent in EF Core?',
              options: [
                'A single database table',
                'A session with the database',
                'A SQL connection string',
                'A migration file'
              ],
              correctIndex: 1,
              explanation: 'A DbContext is a unit-of-work session exposing DbSet<T> collections.'
            },
            {
              id: 'q2',
              prompt: 'Why prefer IQueryable filtering over filtering in memory?',
              options: [
                'It is the only way LINQ works',
                'It pushes the work to the database instead of loading everything',
                'It avoids using async',
                'It disables deferred execution'
              ],
              correctIndex: 1,
              explanation: 'IQueryable translates to SQL so the database does the filtering, transferring less data.'
            }
          ]
        }]
      },
      {
        id: 'csharp-m3',
        title: 'Building REST APIs',
        summary: 'Controllers, routing, and JSON serialization.',
        lessons: [
          {
            id: 'csharp-m3-l1',
            title: 'Working with REST APIs',
            summary: 'Learn how to build and consume REST APIs in ASP.NET Core.',
            duration: '12 min',
            completed: false,
            content: [
              'A REST API exposes resources over HTTP using predictable verbs: GET to read, POST to create, PUT/PATCH to update, DELETE to remove.',
              'In ASP.NET Core, minimal APIs and controllers both map routes to handlers. Return typed results (Results.Ok, Results.NotFound) so status codes are explicit.',
              'Shape responses with DTOs rather than leaking entity classes, and validate input at the boundary before it reaches your domain.'
            ]
          },
          {
            id: 'csharp-m3-l2',
            title: 'Model Binding & Validation',
            summary: 'Turning HTTP requests into typed objects safely.',
            duration: '9 min',
            completed: false,
            content: [
              'Model binding maps query strings, route values, and JSON bodies onto your parameters. Data annotations or FluentValidation enforce rules.',
              'Always treat inbound data as untrusted: validate ranges, required fields, and formats before acting on them.',
              'Return a 400 with a problem-details payload when validation fails so clients get actionable errors.'
            ]
          }
        ],
        quizzes: [{
          isExam: true,
          orderIndex: 1,
          id: 'csharp-m3-quiz',
          title: 'REST APIs Check',
          bestScore: null,
          questions: [
            {
              id: 'q1',
              prompt: 'Which HTTP verb is used to create a new resource?',
              options: ['GET', 'POST', 'DELETE', 'HEAD'],
              correctIndex: 1,
              explanation: 'POST creates; GET reads, PUT/PATCH update, DELETE removes.'
            },
            {
              id: 'q2',
              prompt: 'Why return DTOs instead of entity classes from an API?',
              options: [
                'DTOs are faster to serialize in all cases',
                'To avoid leaking internal data shape and coupling clients to the database',
                'Entities cannot be serialized to JSON',
                'It is required by ASP.NET Core'
              ],
              correctIndex: 1,
              explanation: 'DTOs decouple the wire contract from your persistence model.'
            }
          ]
        }]
      },
      {
        id: 'csharp-m4',
        title: 'Authentication & Security',
        summary: 'JWT, authorization policies, and safe secrets.',
        lessons: [
          {
            id: 'csharp-m4-l1',
            title: 'JWT Authentication',
            summary: 'Issuing and validating JSON Web Tokens.',
            duration: '13 min',
            completed: false,
            content: [
              'A JWT is a signed token carrying claims about the user. The server validates the signature on each request instead of storing session state.',
              'Keep tokens short-lived and pair them with refresh tokens. Never put secrets in the payload — it is encoded, not encrypted.',
              'Register authentication before authorization in the middleware pipeline, and protect endpoints with [Authorize] or policy requirements.'
            ]
          }
        ],
        quizzes: []
      }
    ]
  },
  {
    id: 'python-fundamentals',
    title: 'Python Fundamentals',
    provider: 'By Google',
    description:
      'Start programming with Python — syntax, data structures, and the standard library — with hands-on lessons.',
    thumbnail: 'Py',
    category: 'Backend',
    duration: '8 weeks',
    status: 'inProgress',
    isBookmarked: false,
    enrolledDate: '1 week ago',
    lastAccessed: '1 day ago',
    modules: [
      {
        id: 'py-m1',
        title: 'Getting Started',
        summary: 'Syntax, variables, and the REPL.',
        lessons: [
          {
            id: 'py-m1-l1',
            title: 'Your First Program',
            summary: 'Running Python and printing output.',
            duration: '6 min',
            completed: true,
            content: [
              'Python reads top to bottom and uses indentation instead of braces to define blocks. Consistency matters — pick 4 spaces and stick to it.',
              'The interactive REPL lets you experiment line by line, which is perfect for learning.',
              'print() writes to standard output; f-strings (f"Hello {name}") are the modern way to format text.'
            ]
          },
          {
            id: 'py-m1-l2',
            title: 'Data Structures',
            summary: 'Understanding lists, dictionaries, and sets in Python.',
            duration: '15 min',
            completed: false,
            content: [
              'Lists are ordered and mutable; tuples are ordered and immutable; sets hold unique items; dictionaries map keys to values.',
              'Choose the structure by access pattern: dict for lookups by key, set for membership tests, list for ordered sequences.',
              'Comprehensions ([x*2 for x in items]) build collections concisely and are usually faster than manual loops.'
            ]
          }
        ],
        quizzes: [{
          isExam: true,
          orderIndex: 1,
          id: 'py-m1-quiz',
          title: 'Python Basics Check',
          bestScore: null,
          questions: [
            {
              id: 'q1',
              prompt: 'Which Python structure stores unique, unordered items?',
              options: ['list', 'tuple', 'set', 'dict'],
              correctIndex: 2,
              explanation: 'A set holds unique items with no defined order.'
            },
            {
              id: 'q2',
              prompt: 'How does Python define code blocks?',
              options: ['Curly braces', 'Indentation', 'Semicolons', 'BEGIN/END'],
              correctIndex: 1,
              explanation: 'Python uses indentation to delimit blocks.'
            }
          ]
        }]
      },
      {
        id: 'py-m2',
        title: 'Functions & Modules',
        summary: 'Reusable code and the import system.',
        lessons: [
          {
            id: 'py-m2-l1',
            title: 'Defining Functions',
            summary: 'Parameters, defaults, and return values.',
            duration: '10 min',
            completed: false,
            content: [
              'Functions are defined with def and can take positional, keyword, and default arguments.',
              'Return a value with return; a function with no return yields None.',
              'Keep functions focused and give them descriptive names — the name is documentation.'
            ]
          }
        ],
        quizzes: []
      }
    ]
  },
  {
    id: 'sql-developers',
    title: 'SQL for Developers',
    provider: 'IFA Generated',
    description: 'Query and model relational data with confidence — SELECTs, JOINs, indexes, and transactions.',
    thumbnail: 'SQL',
    category: 'Backend',
    duration: '4 weeks',
    status: 'paused',
    isBookmarked: false,
    enrolledDate: '3 days ago',
    lastAccessed: '3 days ago',
    modules: [
      {
        id: 'sql-m1',
        title: 'Querying Data',
        summary: 'SELECT, WHERE, and ORDER BY.',
        lessons: [
          {
            id: 'sql-m1-l1',
            title: 'SELECT Basics',
            summary: 'Reading rows and columns from a table.',
            duration: '8 min',
            completed: false,
            content: [
              'SELECT chooses columns, FROM names the table, and WHERE filters rows. ORDER BY sorts the result.',
              'Ask only for the columns you need instead of SELECT * — it is clearer and transfers less data.',
              'LIMIT (or TOP) caps the number of rows returned, which is essential for pagination.'
            ]
          },
          {
            id: 'sql-m1-l2',
            title: 'Joining Tables',
            summary: 'Combining related data with JOINs.',
            duration: '12 min',
            completed: false,
            content: [
              'An INNER JOIN returns rows that match in both tables; a LEFT JOIN keeps all rows from the left table even when there is no match.',
              'Join on indexed key columns for performance, and always qualify column names when they appear in more than one table.',
              'Think in sets: a join is a filtered cross-product, not a loop.'
            ]
          }
        ],
        quizzes: [{
          isExam: true,
          orderIndex: 1,
          id: 'sql-m1-quiz',
          title: 'SQL Query Check',
          bestScore: null,
          questions: [
            {
              id: 'q1',
              prompt: 'Which JOIN keeps all rows from the left table even without a match?',
              options: ['INNER JOIN', 'LEFT JOIN', 'CROSS JOIN', 'SELF JOIN'],
              correctIndex: 1,
              explanation: 'A LEFT JOIN preserves unmatched left-side rows, filling the right side with NULLs.'
            }
          ]
        }]
      }
    ]
  },
  {
    id: 'aspnet-fundamentals',
    title: 'ASP.NET Core Fundamentals',
    provider: 'By Microsoft',
    description: 'The building blocks of ASP.NET Core: middleware, dependency injection, and configuration.',
    thumbnail: '.NET',
    category: 'Backend',
    duration: '5 weeks',
    status: 'completed',
    isBookmarked: true,
    enrolledDate: '1 month ago',
    lastAccessed: '2 weeks ago',
    modules: [
      {
        id: 'aspnet-m1',
        title: 'The Request Pipeline',
        summary: 'Middleware and the app lifecycle.',
        lessons: [
          {
            id: 'aspnet-m1-l1',
            title: 'Middleware',
            summary: 'How requests flow through the pipeline.',
            duration: '10 min',
            completed: true,
            content: [
              'Middleware components form a pipeline: each can inspect the request, pass it on, and inspect the response on the way back.',
              'Order matters — authentication runs before authorization, and exception handling wraps everything.',
              'Register middleware in Program.cs with app.Use... calls.'
            ]
          }
        ],
        quizzes: [{
          isExam: true,
          orderIndex: 1,
          id: 'aspnet-m1-quiz',
          title: 'Pipeline Check',
          bestScore: 92,
          questions: [
            {
              id: 'q1',
              prompt: 'In the middleware pipeline, what runs first?',
              options: ['Authorization', 'Authentication', 'Routing to a controller', 'JSON serialization'],
              correctIndex: 1,
              explanation: 'Authentication establishes who the user is before authorization decides what they can do.'
            }
          ]
        }]
      }
    ]
  }
];
