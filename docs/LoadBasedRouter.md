# LoadBasedRouter

A load-balancing actor router that distributes messages to actors based on their current load, ensuring even distribution and avoiding overloading of individual actors. It supports both dynamic load-based routing and round-robin strategies, with snapshot capabilities for monitoring actor load.

## API

### `LoadBasedRouter`

The public constructor for creating a new `LoadBasedRouter` instance.
