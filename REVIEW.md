# Code Review & Debugging Analysis (REVIEW.md)


---

## 1. Summary of Production Failures Found

1. **Missing Cancellation Tokens (`CancellationToken`)**: Asynchronous database calls do not pass down cancellation tokens, preventing the app from aborting requests when clients disconnect, leading to thread and database connection pool exhaustion under load.
2. **Blocking Async Calls (`.Result` or `.Wait()`)**: Synchronously blocking asynchronous tasks leads to thread pool starvation, deadlocks in ASP.NET Core, and severe throughput degradation under high load.
3. **Improper DbContext Lifetime & Concurrency (Captive Dependencies / Thread Safety)**: Using a transient/singleton service incorrectly or sharing a single `DbContext` instance across threads causes severe threading exceptions (`InvalidOperationException`) because EF Core context is not thread-safe.
4. **Unhandled Exceptions in Background Services**: Background tasks running in an infinite loop without proper exception handling or scope management will crash silently or leak resources when transient database faults occur.
5. **Transaction Isolation & Concurrency Control Bugs**: Missing proper retry logic around optimistic concurrency exceptions (`DbUpdateConcurrencyException`) or failing to properly roll back transactions during failures leads to dirty reads, data corruption, and unhandled 500 errors instead of clean 409 Conflict responses.

---

## 2. Detailed Breakdown & Corrected Code

### Problem 1: Lack of Cancellation Token Propagation
* **Why it fails in production:** When clients disconnect or time out, the server continues executing heavy database queries. Under heavy traffic, this exhausts the database connection pool.
* **Correction:** Always accept and propagate `CancellationToken cancellationToken = default` down to all Entity Framework and async methods.

### Problem 2: Thread Starvation via Blocking Async Code
* **Why it fails in production:** Calling `.Result` on an async task blocks the calling thread. In ASP.NET Core thread pools, this quickly causes thread starvation and deadlocks.
* **Correction:** Use `await` consistently all the way up the stack (async/await paradigm).

### Problem 3: Robust Exception & Concurrency Handling
* **Why it fails in production:** Unhandled concurrency conflicts throw raw 500 Internal Server Errors instead of clean 409 Conflict status codes.
* **Correction:** Wrap updates in try-catch blocks handling `DbUpdateConcurrencyException` and rethrow as domain-specific exceptions.

---

## 3. Production-Ready Template Example

Here is how a production-ready repository/service method should look:

```csharp
public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
{
    var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
    if (product == null)
        throw new NotFoundException($"Product with ID {id} was not found.");

    _mapper.Map(dto, product);
    _unitOfWork.Products.Update(product);

    try
    {
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        throw new ConcurrencyConflictException("A concurrency conflict occurred while updating the product.", ex);
    }

    return _mapper.Map<ProductDto>(product);
}