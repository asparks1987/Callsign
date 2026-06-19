# Adding an Automation Tool

This guide applies to the target local automation server.

## 1. Define user outcome

State the exact supported action, target, release, and non-goals. Do not start from a generic OS API.

## 2. Classify

Document:

- Risk tier.
- Privacy classes.
- Reversibility.
- External effects.
- Required session state.
- Approval rule.
- Blocked contexts.

## 3. Specify contract

- `domain.verb_object` name.
- Version.
- Bounded JSON Schema.
- Standard result/error envelope.
- Structured error codes.
- Timeout/cancellation.
- Verification.

## 4. Threat review

Map abuse cases:

- Prompt injection.
- Target ambiguity.
- Path/selector manipulation.
- Context change.
- Sensitive field.
- Elevation.
- Late callback.
- Data leakage.
- Compound side effects.

## 5. Policy first

Add allow, approval, deny, and handoff rules before adapter execution.

## 6. Adapter

Use native API/UIA first. Keep protocol, policy, and OS implementation separate. Never interpolate arguments into a shell.

## 7. Verify

Define a trusted postcondition. If no useful verification exists, the tool is not ready for autonomous execution.

## 8. Audit

Emit redacted request class, policy result, adapter, outcome, verification, duration, and correlation ID.

## 9. Test

- Schema.
- Happy path.
- Denied path.
- Approval.
- Context change.
- Cancellation.
- Timeout.
- Dependency failure.
- Redaction.
- Verification failure.
- Manual visible evidence.

## 10. Document and release

Update tool catalog, risk matrix, security/threat/privacy docs, test plan, ADR if needed, burndown, schemas, examples, and generated site.
