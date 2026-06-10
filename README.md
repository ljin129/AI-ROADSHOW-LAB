# AI Roadshow PC

AI Roadshow PC is a web-based administration platform designed for managing AI-powered roadshow simulations, training activities, assessment scenarios, roles, questions, and competency models.

The project is built with Vue 2, Element UI, Axios, and ECharts, and supports multiple deployment environments for development, testing, QA, pre-production, and production.

---

## Features

### Activity Management

* Create and edit roadshow activities
* Configure activity settings
* Manage activity lifecycle
* View activity statistics and reports

### Ability Dimension Management

* Create competency frameworks
* Manage hierarchical ability dimensions
* Configure evaluation standards

### Question Management

* Maintain question banks
* Configure assessment questions
* Support scenario-based evaluation content

### Role Management

* Create and manage simulation roles
* Configure role-related information

### Scene Management

* Manage simulation scenes
* Configure scene-specific settings
* Associate scenes with activities

### Reporting & Analytics

* Activity statistics dashboard
* Data visualization using ECharts
* Assessment result analysis

---

## Technology Stack

### Frontend

* Vue.js 2.6
* Vue Router 3
* Element UI 2
* Axios
* ECharts

### Development Tools

* Vue CLI 4
* Babel
* ESLint
* Less

### Deployment

* Docker
* Nginx (recommended)

---

## Project Structure

```text
src
├── api/                    # API definitions
├── assets/                 # Static resources
├── axios/                  # Axios configuration
├── common/                 # Common utilities
├── components/             # Shared components
├── router/                 # Route configuration
│   └── modules/
├── utils/                  # Utility functions
├── view/
│   ├── abilityDimension/   # Ability Dimension module
│   └── activityManagement/ # Activity Management module
├── App.vue
└── main.js
```

---

## Environment Configuration

The project supports multiple environments:

```text
.env.dev
.env.test
.env.qa
.env.pre
.env.pro
```

Configure API endpoints and environment-specific variables in the corresponding files.

---

## Installation

### Prerequisites

* Node.js >= 14
* npm >= 6

### Install Dependencies

```bash
npm install
```

---

## Development

Start the local development server:

```bash
npm run dev
```

or

```bash
npm run serve
```

---

## Build

### Test Environment

```bash
npm run buildtest
```

### QA Environment

```bash
npm run buildqa
```

### Pre-production Environment

```bash
npm run buildpre
```

### Production Environment

```bash
npm run build
```

---

## Docker Deployment

Build Docker image:

```bash
docker build -t airoadshow-pc .
```

Run container:

```bash
docker run -d -p 80:80 airoadshow-pc
```

---

## Main Dependencies

| Package    | Version |
| ---------- | ------- |
| Vue        | 2.6.14  |
| Vue Router | 3.2.0   |
| Element UI | 2.15.3  |
| Axios      | 1.8.4   |
| ECharts    | 5.3.0   |
| SortableJS | 1.15.6  |

---

## Available Modules

| Module               | Description                  |
| -------------------- | ---------------------------- |
| Activity Management  | Manage roadshow activities   |
| Ability Dimension    | Maintain competency models   |
| Question Management  | Manage assessment questions  |
| Scene Management     | Configure business scenarios |
| Role Management      | Configure simulation roles   |
| Statistics Dashboard | Analyze activity data        |

---

## Coding Standards

* ESLint for code quality
* Prettier for formatting
* Component-based architecture
* Modular routing structure
* API abstraction layer

---

## License

This project is intended for internal enterprise use.

All rights reserved.
