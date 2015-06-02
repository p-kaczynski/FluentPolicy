Fluent Policy
=============

## Purpose
FluentPolicy offers a fluent syntax for creating policies concerning handling exceptions and return values from synchronous and asynchronous sources. It can be used to remove a lot of boilerplate code and allow methods to focus on what's actually relevant to them.

This library is heavily inspired by [Polly](https://github.com/michael-wolfenden/Polly). As Polly is much more mature solution I recommend using it, if the functionality offered is sufficient.
## Usage
Entry point to FluentPolicy is either a Func<T> or Task<T> for respectively synchronous or asynchronous mode of operations. To create a policy for them use `WithPolicy` extension methods. This will give you access to `For()` method that allows specifying conditions - either an exception that was thrown, or a return value was obtained and needs to be checked.

Specified conditions are checked top to bottom, and first matching one gets executed. Possible actions vary depending on whether the condition was exception or return value based. Available action are: `Throw`, `Return`, `Rethrow` (for exceptions only), `Retry` and `WaitAndRetry` (currently for exceptions only, but that's to be extended for return values as well).
### Synchronous
#### From Func
```
Func<int> getInteger = () => 5;
var result = getInteger.WithPolicy()
            .For().Exception<MyCustomException>(mce=>mce.ErrorCode = 1).Return(-1)
            .For().Exception<MyCustomException>(mce=>mce.ErrorCode = 2).Return(-2)
            .For().Exception<MyCustomException>().Return(-3)
            .For().AllOtherExceptions().Rethrow()
            .Execute();
```
#### From Method
Unfortunatelly currently it is not possible to create an extension method for a method, so a helper is needed:
```
public int GetInteger(){
    return 5;
}
var result = As.Func(GetInteger).WithPolicy()
            .For().Exception<MyCustomException>(mce=>mce.ErrorCode = 1).Return(-1)
            .For().Exception<MyCustomException>(mce=>mce.ErrorCode = 2).Return(-2)
            .For().Exception<MyCustomException>().Return(-3)
            .For().AllOtherExceptions().Rethrow()
            .Execute();
```
### Asynchronous
To use with asynchronous code just use `WithAsyncPolicy` and `ExecuteAsync` - rest of the methods stays the same
```
public async Task<int> GetIntegerAsync(){
	return await Task.FromResult(5);
}

var result = await As.Func(GetIntegerAsync).WithAsyncPolicy()
			.For().Exception<MyCustomException>(mce=>mce.ErrorCode = 1).Return(-1)
            .For().Exception<MyCustomException>(mce=>mce.ErrorCode = 2).Return(-2)
            .For().Exception<MyCustomException>().Return(-3)
            .For().AllOtherExceptions().Rethrow()
            .ExecuteAsync();
```