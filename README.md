# Decisions

For this code exercise I decided to user DDD and port and adapters because it generally lends itself to making the
code easier to understand for users that are new to the repo and it's fresh in my mind.

It also provides enough flexibility by separating the different layers where they can continue to be independently
evolved without necessarily driving change in the other layers. Though that risk can only be mitigated so much, there
is always some level of bleed across layers.

I left the scope relatively small given the time frame and the requirements being vague.

I chose C# and tooling because of expediency and familiarity. Obviously if this solution was part
of a larger organization I imagine we would align with the stack that better aligned with what the organization user.

I picked SQLite because I know it can be spun up quickly, and it can run in memory. The table structure mapped 1:1 with
the aggregates so it kept it simple. Since they were abstracted by a IRepository interface It's something it can be
optimized / replaced later if needed

The tradeoffs are around time to build it and general ambiguity on the requirements. I could go very deep
try to guess what the app should be. However, I am a firm believer that technology and implementation should be
driven by the requirements and the problems that need to be solved. Given the scope provided this seemed sufficient
to get the point across.

## Concurrency:
Currently implemented with optimistic concurrency, but more complex use cases could be implemented like allowing multiple
users working in the notes at the same time. For this example a simple cas to prevent concurrency contention seemed sufficient
I went with the simplest approach, however concurrency and multi-user editing could be improving by adding support for drafts
and versions. This would also mean adding more aggregates supporting infrastructure

## Auth:
Like any app with resources and ownership you need a way to authenticate and authorize your users. A simple jwt with roles and user
Id was plenty. I implemented in app to support the behavior. I imagine in a real app an external auth provider would be used. Keeping
the high level access in roles tends to work. But in my experience when more intricate rules are needed roles in a jwt reach
their size limit. This leads to some sort of permission management system ends up being created

## NoteAccessPolicy:
Seems that the notes aggregate could get overloaded and keeping access to notes in a separate entity made sense at the moment
It was a quick way to be better aligned with single responsibility purposes. This would also allow this logic to be improved
in the future without really affecting the core note functionality

### Multimedia
Notes with attachments would probably lead to claims check patterns where we can keep notes in a separate blob with
links to the records on the db

### The db would need to be optimized on how it's going to be accessed
We could go nosql and just create materialized views, that would allow for quick lookup of notes. We could leverage
SQLite indexes and stored procs to organize it. Again usage and requirements should drive this decision

### Eventing
Could be added behind the scenes, whether in memory or as an external channel like an event stream or event bus
This could open the door to notification, job processing and other functionality

### If we implement notifications
We could also add websockets or something like signalR to support multiple users editing
the same note and seeing the changes live in their screen on their apps

### Development Experience
I set up aspire to get the app running easily. I am a firm believer that it really helps sharing projects and boarding
if the barrier to get the project running and setup is lowered. Having a single command to run to have the app running really helps

### Testing and Test Data
I added a set of sample data to allow the app to be easily tested and run. This not only helps developers but also agents
if we want to start introducing agentic workflows. Testing could also be improved for adding unit, integration, and performance

### Performance
I used DDD and a lot of OOP but if we wanted higher performance and throughput we could always optimize
for the performance we want. Whether it is we de-nest objects, focus on allocations, start reducing loops, look at how we
return the data to optimize serialization
