# Spektra Games Case Study

I used .NET 10 for the server and PostgreSQL as the database choice for this case study.  
OpenApiDocument.json is in the root folder of the repo.  

I also added the Scalar API interface in prod environment too so it would be easier to check the endpoints  
<http://localhost:8080/scalar/>

## Authentication

I used Microsoft Identity API for auth flow. I modified it a bit for our needs. I disabled the original `/auth/register` endpoint and added a `/auth/signup` endpoint. In that endpoint, you need to post an email and a strong password. When you signup, backend will create a TenantId for you and you become the admin of that tenant. A new `/signup` request would create a new tenant with a new admin.
> `/register` endpoint is visible in OpenAPI docs but it just redirects to 404 Not Found. There is ways to remove it from there but the hassle is not worth it.

You can log in with the `/auth/login` endpoint. It would give you your encrypted Bearer token. This token is used for all the authorization.

API also includes support for 2FA but I didn't make it mandatory, the endpoints are active for enabling 2FA for users.

For adding new members to your tenant, you can use `/add-member` endpoint with your Bearer token. Your token includes your TenantId and your Admin status for that tenant. The endpoint checks if you are the admin of that tenant, if so, a new member is created with the same TenantId, but not as an admin, so that user can't add new users. All members of the tenants can make requests for data of that tenant.

## Database Schema

Beside the User management tables created by Microsoft Identity, I created one table for the Aggregate data, the primary key is the composition of TenantId, UserId and EventName. So same user could be in multiple tenant's data and could trigger the same event and still be a separate record. Each row holds these data:

```
TenantId    |
UserId      | -> Composite Key
EventName   |

SumValue
MinValue
MaxValue

FirstTs
FirstValue
LastTs
LastValue
```

## Idempotency Strategy

Every incoming `/event/ingest` request has an EventId. The server has a `ProcessedEvents` dictionary that saves the EventId with the timestamp of that event. Whenever a new event comes, the `IsDuplicateOrTooOld` validation checks the event and tries to find if that EventId is already processed, or is the event older than a period of time. This period could be specified with the appsetting.json or from environment variables.

## Handling Out-of-order Timestamps

If an out-of-order event is not processed before, it gets processed like a normal event. But there is checks for existing data's LastTs or FirstTs. If a newer event's timestamp is older than that event's existing aggregate's LastTs, event's values are processed like it should be, adding to sum_value and setting the min or max if necessary, but the LastTs variable doesn't change since it is an older data than the current LastTs.

## Performance Improvements

For performance, the server isn't sending a `CREATE` request to the DB for every new ingestion. The server has a `Hot Storage` in it's memory. A concurrent dictionary with "primary key" as the key and the aggregate data as the value. Whenever a new `/event/ingest` request comes, the data is saving to that dictionary. With this, we can make incredibly fast read and write operations. If an aggregate is used in constantly in a short period of time, rather than asking to the DB for that data, we can get the data from this hot storage.

Or whenever a query is made through `/events/aggregate` or `/events/evaluate`, the server first checks that hot storage and only sends a read operation to the DB if it can't find it in there.

Also whenever an `/event/ingest` request makes a change in a data, the key of that data is saved to a `MarkedKeys` list (*queue*). And we have a worker that works in every 2 seconds, checking if there is any data change. Even if we have 50 thousand data in our hot storage, with the `MarkedKeys` list, we know which data are changed in last 2 seconds and send those data to the DB as a single bulk upsert method. With this, we can achieve so much throughput with less DB calls.

## Reset/Cleanup Strategy

Every minute, a cleanup worker runs and checks for the `Hot Storage` and `ProcessedEvents` to delete cold data (data that didn't change in last 10 minutes) and processed events that is older than 10 minutes. This is for reducing RAM usage.

These all numbers are configurable via appsettings.json or environment variables.

## Benchmark

Using [wrk](https://github.com/wg/wrk), I ran a benchmark to test all the endpoints. These are the results on my local machine:

> 2018 Thinkpad T480, i5 8350U CPU, 16GB RAM  
> benchmark.lua file is in the repo.

Using 4 threads and 200 concurrent users, my API could handle ~30k requests per second with less than < 10ms latency on average.

> 70% of the requests was `ingest` operations. If the query requests were more frequent, the results would be even higher.  

```bash
wrk -t4 -c200 -d30s -s benchmark.lua <http://localhost:8080>
Running 30s test @ <http://localhost:8080>
4 threads and 200 connections
Thread Stats   Avg      Stdev     Max   +/- Stdev
Latency     6.73ms    2.94ms  62.34ms   80.16%
Req/Sec     7.45k   625.61    11.56k    76.31%
890424 requests in 30.08s, 115.19MB read
Requests/sec:  29603.99
Transfer/sec:      3.83MB
```

> Logging is disabled in this benchmark. Server responses so fast that the only bottleneck is the logging. When enabled, results are about ~20k req/sec.  
> Since we use a hot storage for frequent data access, server becomes faster as the benchmark goes on, because more data gets into the hot storage. This benchmark result is the result of running the benchmark a few times to warm up server.

And the ram usage of Server and the database during the peak of the benchmark was like this:

```
Server:     308 MB
PostgreSQL: 146 MB
```

## Behaviour When A Data Is Not Found

If there is no record for a queried UserId/Event pair, instead of returning a 404 Not Found, the server returns a record with all values set to 0. This makes sense for this application's use case because if there is no `purchase` event for a given user, that means the sum_value (and min or max) of that user's purchase value is 0, not Null;

Also, If a record is not found, instead of creating an object with all fields set to 0, I used a struct so there won't be any heap allocation just for returning an empty data to save on allocation time and Garbage Collection time.

## AI Usage

I came up with all the methods to improve the performance of this project. I wrote the workflow and main business, then got help from AI to structure the project better. Also I started the project with SQLite to prototype easier, and when migrating to PostgreSQL, I got help from the AI to write the bulk upsert method. I used bulk upsert's with MongoDB when I was an intern but didn't know how to do bulk upsert in PostgreSQL so I learned it with the help of AI.

Also got help from it to write the benchmark.lua  
I wrote this Readme.md all by myself without any help from AI.

## Trade-offs

I prioritized performance for this project and per the CAP theorem, there are some consequences for these choices. For example the new data is written to the DB every 2 seconds, so if server crashes in a moment, the changed data in that window is gone. But of course if we would deploy this project to a distributed server for production use, instead of saving the updated keys in the memory, it would be wiser to use something like RabbitMQ to save the queue of changed data and save them to DB in an interval.

Also for Hot Storage and ProcessedEvents, using Redis would be a better choice for reliability to both not having a cold startup when server restarts to fill the hot storage, since Redis already keeps them, and not lose the ProcessedEvents in the last 10 minutes.
