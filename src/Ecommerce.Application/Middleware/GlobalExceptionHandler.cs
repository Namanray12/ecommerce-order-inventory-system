//using Ecommerce.Domain.Exceptions;
//using Microsoft.AspNetCore.Diagnostics;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Logging;
//using System.Web.Http.ExceptionHandling;

//namespace Ecommerce.API.Middleware
//{
//    public class GlobalExceptionHandler : IExceptionHandler
//    {
//        private readonly ILogger<GlobalExceptionHandler> _logger;

//        public GlobalExceptionHandler(
//            ILogger<GlobalExceptionHandler> logger)
//        {
//            _logger = logger;
//        }

//        public async ValueTask<bool> TryHandleAsync(
//            HttpContext httpContext,
//            Exception exception,
//            CancellationToken cancellationToken)
//        {
//            _logger.LogError(
//                exception,
//                "An exception occurred: {Message}",
//                exception.Message);

//            var problemDetails = new ProblemDetails
//            {
//                Instance = httpContext.Request.Path
//            };

//            switch (exception)
//            {
//                case NotFoundException ex:
//                    problemDetails.Status = StatusCodes.Status404NotFound;
//                    problemDetails.Title = "Not Found";
//                    problemDetails.Detail = ex.Message;
//                    break;

//                case UnauthorizedException ex:
//                    problemDetails.Status = StatusCodes.Status401Unauthorized;
//                    problemDetails.Title = "Unauthorized";
//                    problemDetails.Detail = ex.Message;
//                    break;

//                case ForbiddenAccessException ex:
//                    problemDetails.Status = StatusCodes.Status403Forbidden;
//                    problemDetails.Title = "Forbidden";
//                    problemDetails.Detail = ex.Message;
//                    break;

//                case ConcurrencyConflictException ex:
//                    problemDetails.Status = StatusCodes.Status409Conflict;
//                    problemDetails.Title = "Conflict";
//                    problemDetails.Detail = ex.Message;
//                    break;

//                case DomainException ex:
//                    problemDetails.Status = StatusCodes.Status400BadRequest;
//                    problemDetails.Title = "Bad Request";
//                    problemDetails.Detail = ex.Message;
//                    break;

//                default:
//                    problemDetails.Status =
//                        StatusCodes.Status500InternalServerError;
//                    problemDetails.Title = "Internal Server Error";
//                    problemDetails.Detail =
//                        "An unexpected error occurred.";
//                    break;
//            }

//            httpContext.Response.StatusCode = problemDetails.Status.Value;

//            await httpContext.Response.WriteAsJsonAsync(
//                problemDetails,
//                cancellationToken);

//            return true;
//        }
//    }
//}
