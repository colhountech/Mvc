using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ErrorDescription;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ProblemFilter : IResultFilter
    {
        private readonly IErrorDescriptionFactory _factory;

        public ProblemFilter(IErrorDescriptionFactory factory)
        {
            _factory = factory;
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (!(context.Result is StatusCodeResult statusCodeResult))
            {
                return;
            }
            var statusCode = statusCodeResult.StatusCode;
            if (statusCode == StatusCodes.Status400BadRequest)
            {
                var problem = _factory.CreateErrorDescription(context.ActionDescriptor, new ProblemDescription
                {
                    Status = statusCodeResult.StatusCode,
                    Title = "400 Bad Request",
                });

                context.Result = new BadRequestObjectResult(problem)
                {
                    StatusCode = statusCodeResult.StatusCode,
                };
            }
            else if (statusCode == StatusCodes.Status404NotFound &&
                context.ActionDescriptor.Parameters.Any(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase)))
            {
                var problem = _factory.CreateErrorDescription(context.ActionDescriptor, new ProblemDescription
                {
                    Status = statusCodeResult.StatusCode,
                    Title = "No value found for the specified id.",
                });
                context.Result = new BadRequestObjectResult(problem)
                {
                    StatusCode = statusCodeResult.StatusCode,
                };
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}
