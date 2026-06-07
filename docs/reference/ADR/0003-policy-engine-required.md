# ADR-0003: Policy engine required for all action tools

## Status

Accepted for MVP.

## Context

The LLM may misunderstand intent, be prompt-injected, or request unsafe actions. Tool schemas alone are not sufficient authorization.

## Decision

Every action tool must call the policy engine before execution.

## Rationale

- Authorization must be outside the model.
- Risk tiers need consistent enforcement.
- Approval prompts must be generated from policy, not improvised by the model.
- Safety tests need a single decision boundary.

## Consequences

- Tools require metadata about risk, reversibility, and privacy.
- Policy tests are mandatory for action tools.
- The MVP can safely start with a small strict policy set.
