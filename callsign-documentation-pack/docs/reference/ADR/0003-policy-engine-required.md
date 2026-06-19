# ADR-0003: Policy engine required for every action

## Status

Accepted.

## Context

Models, prompts, tools, and observed content can be wrong or manipulated. Tool schemas validate shape, not permission.

## Decision

Every action passes through a deterministic policy engine outside the model and adapter.

## Consequences

- Unknown capability fails closed.
- Tools publish risk, privacy, reversibility, approval, and verification metadata.
- Approval prompts are policy outputs.
- Policy tests are mandatory.
- The model cannot override denial.
- Degraded audit/policy health can disable state-changing actions.
