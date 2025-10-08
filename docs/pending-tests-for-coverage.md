# Pending Tests to Approach 100% Coverage for eShopOnWeb

## Introduction
This document outlines concrete, actionable test scenarios organized by module to significantly improve code coverage for eShopOnWeb. It is based on the current codebase structure under `src/` and the existing test suites under `tests/` (Unit, Integration, Public API Integration, and Functional tests). Current coverage is low (approximately 12.89% line and 3.70% branch), indicating substantial room for improvement, especially around branches, error handling, and authorization. The following scenarios focus on high-value paths and branch-heavy logic.

## ApplicationCore

### Entities

#### BasketAggregate
- Basket.cs
  - AddItem: add duplicate item with same `catalogItemId` to ensure quantity increments; add with negative quantity throws/guard behavior; add with zero quantity no-op behavior.
  - RemoveEmptyItems: verify removal when quantities are set to zero through various interleavings (multiple items, edge cases with large quantities).
  - SetNewBuyerId: transfer buyer ID after existing non-empty state; idempotency when setting to same buyer; behavior when basket has null/empty buyer.
  - TotalQuantity: verify with large baskets and mix of quantities; ensure performance and correct summation.
- BasketItem.cs
  - Unit price and quantity guards: negative and zero values; verify value normalization or exception paths.
  - Decrement/Increment semantics (if present) and boundaries (min/max).

#### OrderAggregate
- Order.cs
  - Constructor guard paths for null address, empty items, zero/negative price/quantity.
  - Total calculation: rounding behavior, multiple currencies (if represented), mixed quantities and unit prices; large inputs.
  - AddItem/ReplaceItem behavior (if present), equality semantics for line items.
- OrderItem.cs
  - Validation for `CatalogItemOrdered` snapshot: null name or invalid picture URI.
  - Branches for updating quantity or price (if method paths exist).
- CatalogItemOrdered.cs
  - Guard clauses for invalid Id/Name/PictureUri; string trimming and normalization; URI composition correctness (with/without trailing slashes).

#### BuyerAggregate
- Buyer.cs
  - Adding a PaymentMethod: duplicate aliases; updating existing method; invalid card details; last4 formatting.
- PaymentMethod.cs
  - Guards for long alias, invalid last4; equality semantics (value object) and hash code consistency.

### Services

#### BasketService.cs
- AddItemToBasket
  - Creates new basket when none exists; adds to existing basket; de-duplicates same item; rejects invalid catalog item IDs.
  - Error paths when repository throws (DB unavailable) and logging occurs via IAppLogger.
- SetQuantities
  - Removes items when quantity set to zero; handles unknown item ids; rejects negative quantity; atomic update of multiple items.
- TransferBasket
  - Transfers from anonymous to user; handles existing destination basket (merge quantities and deduplicate); handles missing source; idempotent second transfer.
- DeleteBasket
  - Behavior when basket does not exist; verifies repository Delete invocation and logging branch.

#### OrderService.cs
- CreateOrder
  - EmptyBasketOnCheckoutException thrown for empty basket; BasketNotFoundException thrown for invalid basket id.
  - Partial catalog availability: one or more catalog items missing; price mismatches; ensures transactional consistency.
  - Address validation branch coverage; ensures created order contains snapshot of items.
- UriComposer.cs
  - Various base URL configurations (with/without trailing slash); relative vs absolute picture name; null/empty settings.

### Specifications
- CatalogFilterSpecification, CatalogFilterPaginatedSpecification
  - Filter combinations: only brand, only type, both, none; pagination boundaries; pageIndex out-of-range handling; order-by clause if present.
- BasketWithItemsSpecification
  - Constructor paths: by basketId and buyerId; includes navigation loading behavior.
- CustomerOrdersSpecification, CustomerOrdersWithItemsSpecification
  - Includes and criteria application verified; empty result behavior; invalid buyer id.

### Exceptions
- BasketNotFoundException, EmptyBasketOnCheckoutException, DuplicateException
  - Ensure thrown in all relevant services; message content and inner exception propagation.

### Extensions
- GuardExtensions.cs
  - Verify Guard.Against.EmptyBasketOnCheckout triggers for empty baskets; no-op for non-empty; null basket handling.

- JsonExtensions.cs
  - Serialization options (case-insensitive) verified; round-trip serialization for complex objects; invalid JSON error paths.

### Interfaces Adapters
- LoggerAdapter<T>, IAppLogger
  - Logging methods invoked on information/warning paths; ensure generic category used and messages contain correlation details when applicable.

## Infrastructure

### Data Layer
- CatalogContext.cs and EF Configurations (CatalogBrandConfiguration, BasketConfiguration)
  - Verify schema rules via integration tests using EF InMemory/SQLite: required fields, max lengths, cascades, deletes, relationships.
  - Basket-Items relationship: removing item updates DB; deleting basket removes items (or verifies configured behavior).
- EfRepository.cs
  - GetById, List, ListAsync with specifications including Includes are materialized; Add/Update/Delete behavior; handling of null entity; concurrency exceptions simulated.
  - Branch coverage for IEnumerable vs IQueryable paths (if any).
- FileItem.cs
  - Validation for file metadata (size, extension, type); handling of missing base64 data.
- Migrations Snapshots
  - Not directly unit tested; indirectly verified via DbContext model validation in integration tests.

### Identity
- AppIdentityDbContext.cs
  - Model configuration and relationships (users, roles, claims) verified through smoke integration tests.
- AppIdentityDbContextSeed.cs
  - Idempotent seeding: re-run seed does not duplicate roles/users; errors logged when role/user creation fails; seeded admin user can authenticate.
- IdentityTokenClaimService.cs
  - Token creation includes required claims (sub, unique_name, roles); missing user throws UserNotFoundException; no roles yields token without role claims; expired/invalid signing keys paths (if injectable).
- ApplicationUser.cs, UserNotFoundException.cs
  - Ensure exception used in service; application user extra fields default behavior.

### Services
- EmailSender.cs
  - Ensure method is invoked by workflows (stub) and logs appropriately; handle exceptions silently or rethrow per design.

## PublicApi

### Program.cs
- Middleware registration order; Swagger enabled in Development; seeding invoked at startup without duplicate roles/users; health checks endpoints are mapped.
- Error handling for failed DB migrations; configuration binding (CatalogSettings).

### Middleware
- ExceptionMiddleware.cs
  - Returns proper status code and JSON shape for known exceptions (e.g., BasketNotFoundException, EmptyBasketOnCheckoutException, validation errors); generic 500 for unhandled exceptions; correlation ID propagation; logging branches.

### Endpoints: CatalogBrandEndpoints, CatalogTypeEndpoints
- List endpoints
  - Empty results; paging (if supported); mapping to DTOs; repository failures; authorization (if required) and 401/403 paths.

### Endpoints: CatalogItemEndpoints
- GetById
  - Valid/invalid id; unauthorized vs authorized (if required); mapping including composed picture URI.
- ListPaged
  - Filters: brandId only, typeId only, both, none; pageIndex/pageSize edge cases; total count and page count correctness; parallel query behavior and throttling (if any).
- Create
  - Validation failures (missing name, negative price); duplicate name/brand/type behavior; created response fields; 401/403 without admin token.
- Update
  - Non-existent item yields 404; partial updates vs full; validation errors; concurrency conflict (etag or version if applicable).
- Delete
  - Non-existent returns 404; delete when item associated with order (if business rule applies); 401/403 without admin token.

### AuthEndpoints
- Authenticate
  - Invalid credentials; locked out/disabled user; missing roles; multi-role token generation; token expiry and refresh (if supported by API).

### DTOs and Base Types
- BaseRequest/BaseResponse/BaseMessage
  - Correlation ID generation and propagation; constructors with/without correlation ID; JSON serialization of the response types.

## Web (MVC/Razor)

### Controllers
- AccountController
  - Sign-in successes/failures; anti-forgery token validation; redirect paths; invalid model state.
- OrderController
  - AuthZ enforced (anonymous redirected); listing orders with/without items; error handling for repository failures.

### Pages and Components
- Home page
  - Catalog listing pagination; empty catalog state; error banner on backend failure.
- Basket pages
  - Add, update, remove flows across combinations; empty basket view; checkout redirect when unauthenticated vs authenticated.
- CacheHelpers and Web Extensions
  - Cache key generation with various inputs; ensure stable keys used across pages.

## BlazorAdmin

### Services and Decorators
- CachedCatalogItemServiceDecorator
  - Cache hit/miss logic; eviction/expiration behavior; logging branches; local storage failure handling.
- CachedCatalogLookupDataServiceDecorator
  - Cache key formation by generic type; null/empty payload handling; refresh invalidation.
- CatalogLookupDataService
  - HTTP errors; serialization errors; retry behavior if present.

### UI Components and Helpers
- CustomInputSelect
  - Parsing failures; non-integer values; binding round-trips and validation.
- ToastService
  - Show/hide timing and levels; overlap behavior; cancellation when hidden early.
- RefreshBroadcast/BlazorComponent
  - Subscription/unsubscription; dispose semantics; multiple subscribers.

### JavaScript Interop
- Cookies, Route, Css, JSInteropConstants
  - Interop failure behavior; null JSRuntime; invocation identifiers and parameter passing; side-effect validation in DOM (if feasible via bUnit + JSInterop).

## Cross-Cutting

### Configuration and Settings
- CatalogSettings binding via Options; missing or malformed configuration scenarios.

### Logging
- Ensure log statements executed on error paths and important events across services and middleware.

### Security and Authorization
- Role-based endpoints return 403 without appropriate role; JWT validation failure; expired token; missing bearer token; policy-based requirements on Web controllers.

## Test Strategy and Types

### Unit Tests
- Focus on pure domain logic (ApplicationCore Entities, Services), specifications, and DTO behavior.
- Exercise guard clauses and all branches leading to exceptions.

### Integration Tests
- Repository operations with EF Core using InMemory or SQLite for relational behaviors.
- Identity seeding and token generation workflows.

### Public API Integration Tests
- End-to-end endpoint testing with in-memory hosting; validate response codes, payload shapes, and auth enforcement.

### Functional Tests (Web)
- MVC and Razor Pages flows including login, basket interactions, checkout, and redirects.

## Prioritization Guidance for Fast Coverage Gains
1. ApplicationCore domain and services (high branch density and business rules).
2. PublicApi endpoints and middleware error handling paths.
3. Infrastructure repository and identity seeding/token service integration points.
4. Web controllers and key pages for anonymous vs authenticated flows.
5. BlazorAdmin service decorators and caching branches.

## Suggested Test File Placements
- tests/UnitTests/ApplicationCore/Entities/... for entity tests.
- tests/UnitTests/ApplicationCore/Services/... for service tests.
- tests/UnitTests/ApplicationCore/Specifications/... for specifications.
- tests/IntegrationTests/Repositories/... for data layer tests.
- tests/PublicApiIntegrationTests/... for endpoint tests.
- tests/FunctionalTests/Web/... for MVC/Razor flows.
- tests/UnitTests/BlazorAdmin/... for interop and services.

## Notes Based on Current Artifacts and Observations
- Coverage is currently aggregated from multiple Cobertura XMLs but remains low, indicating many modules above are under-tested.
- Duplicate role warnings in tests point to AppIdentityDbContextSeed idempotency issues; add tests to ensure seed is safe to run multiple times.
- xUnit2013 analyzer suggestions imply some assertions can be strengthened; incorporate while adding scenarios to improve branch coverage.

## Example Test Scenario Stubs (Illustrative)
- ApplicationCore.Services.BasketService.SetQuantities removes zero-quantity items:
  - Given a basket with items A(2), B(1)
  - When SetQuantities([{A, 0}, {B, 3}])
  - Then A is removed, B has quantity 3, repository Update called once.

- PublicApi.CatalogItemEndpoints.Create validates negative price:
  - Given admin token and body with Price = -1
  - When POST /catalog-items
  - Then 400 BadRequest with validation error for Price.

- Infrastructure.EfRepository.ListAsync with include specification:
  - Given CatalogItems with related Types
  - When ListAsync(new Spec with Include Type)
  - Then returned items have Type populated.

## Conclusion
The above scenarios, when implemented, will materially increase both line and branch coverage. Emphasize branch-heavy paths (guards, error handling, authorization, caching) and core business logic first for the greatest impact.
