# Recovery

Journals and locks are external to the Unity project.

The external root is derived from:

- stable project id;
- SHA-256 of the canonical project root.

The journal records:

- execution id;
- manifest id;
- writer id;
- process id;
- process start time;
- plan hash;
- completed operations;
- pending operations;
- inverse payloads;
- heartbeat;
- lease expiry;
- completion state.

Incomplete journals cause `recover-status` to report recovery required. New mutating applies are blocked until recovery is resolved.

The executor acquires the OS-exclusive lock, rechecks recovery, and completes handler preflight before
creating the journal. Journal replacement is atomic and journal discovery is restricted to the
external journal subtree.

The current source slice inspects recovery and writes journals only when an apply has passed approval
and handler preflight. Real resume/rollback decisions are deferred.
