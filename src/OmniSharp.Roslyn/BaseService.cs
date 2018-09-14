using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OmniSharp.Mef;
using OmniSharp.Models;

namespace OmniSharp.Roslyn
{
    public abstract class BaseService<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
        where TRequest : SimpleFileRequest
    {
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly OmniSharpWorkspace Workspace;

        protected BaseService(ILoggerFactory loggerFactory, OmniSharpWorkspace workspace)
        {
            LoggerFactory = loggerFactory;
            Workspace = workspace;
        }

        protected abstract Task<TResponse> HandleRequestAsync(TRequest request);

        protected Document GetDocument(TRequest request)
            => Workspace.GetDocument(request.FileName);

        public Task<TResponse> Handle(TRequest request)
        {
            return HandleRequestAsync(request);
        }
    }
}
