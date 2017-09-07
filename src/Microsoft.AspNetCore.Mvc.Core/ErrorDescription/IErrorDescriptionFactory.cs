using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ErrorDescription
{
    public interface IErrorDescriptionFactory
    {
        object CreateErrorDescription(ActionContext actionContext, object result);
    }

    public interface IErrorDescriptorProvider
    {
        int Order { get; }

        void OnProvidersExecuting(ErrorDescriptionContext context);

        void OnProvidersExecuted(ErrorDescriptionContext context);
    }

    public class ErrorDescriptionContext
    {
        public ErrorDescriptionContext(ActionContext actionContext)
        {
            ActionContext = actionContext;
        }

        public ActionContext ActionContext { get; }

        public object Result { get; set; }
    }

    public class DefaultErrorDescriptorFactory : IErrorDescriptionFactory
    {
        private readonly IErrorDescriptorProvider[] _providers;

        public DefaultErrorDescriptorFactory(IEnumerable<IErrorDescriptorProvider> providers)
        {
            _providers = providers.OrderBy(p => p.Order).ToArray();
        }

        public object CreateErrorDescription(ActionContext actionContext, object result)
        {
            var context = new ErrorDescriptionContext(actionContext)
            {
                Result = result,
            };

            for (var i = 0; i < _providers.Length; i++)
            {
                _providers[i].OnProvidersExecuting(context);
            }

            for (var i = _providers.Length - 1; i >= 0; i--)
            {
                _providers[i].OnProvidersExecuted(context);
            }

            return context.Result ?? result;
        }
    }

    public class ProblemErrorDescriptionProvider : IErrorDescriptorProvider
    {
        public int Order => -1000;

        public void OnProvidersExecuted(ErrorDescriptionContext context)
        {
            if (context.Result is StatusCodeResult statusCodeResult)
            {
                var statusCode = statusCodeResult.StatusCode;
                if (statusCode == StatusCodes.Status400BadRequest)
                {
                    var problem = new ProblemDescription
                    {
                        Status = statusCodeResult.StatusCode,
                        Title = "400 Bad Request",
                    };
                    context.Result = problem;
                }
                else if (statusCode == StatusCodes.Status404NotFound && 
                    context.ActionContext.ActionDescriptor.Parameters.Any(p => string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase)))
                {
                    var problem = new ProblemDescription
                    {
                        Status = statusCodeResult.StatusCode,
                        Title = "No value found for the specified id.",
                    };
                    context.Result = problem;
                }
            }
        }

        public void OnProvidersExecuting(ErrorDescriptionContext context)
        {
        }
    }
}
