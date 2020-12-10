## Implementation of outbox pattern in .Net Core

###Description

Outbox pattern was implemented according to:
- https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/subscribe-events#designing-atomicity-and-resiliency-when-publishing-to-the-event-bus

- https://microservices.io/patterns/data/transactional-outbox.html

###Description
How to use?
Please, check out Outbox.Examples Project

Keep that in mind that each client can have own implementation of bus publisher. Therefore, IBusPublisher was defined as a separated interface.
https://martinfowler.com/eaaCatalog/separatedInterface.html

This implementation of outbox pattern can be successfully used in a production environment.

###Requirements

.Net Core 3.1, but it can be downgraded. A job responsible for sending was implemented as a .net core background service, but you can program your own job implementation easily (please, take a look at Outbox.Tests Project)