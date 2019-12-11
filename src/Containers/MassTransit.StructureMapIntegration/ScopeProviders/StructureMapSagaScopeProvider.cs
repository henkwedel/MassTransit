namespace MassTransit.StructureMapIntegration.ScopeProviders
{
    using System;
    using System.Collections.Generic;
    using Context;
    using GreenPipes;
    using Saga;
    using Scoping;
    using Scoping.SagaContexts;
    using StructureMap;


    public class StructureMapSagaScopeProvider<TSaga> :
        ISagaScopeProvider<TSaga>
        where TSaga : class, ISaga
    {
        readonly IContainer _container;
        readonly IContext _context;
        readonly IList<Action<ConsumeContext>> _scopeActions;

        public StructureMapSagaScopeProvider(IContainer container)
        {
            _container = container;
            _scopeActions = new List<Action<ConsumeContext>>();
        }

        public StructureMapSagaScopeProvider(IContext context)
        {
            _context = context;
            _scopeActions = new List<Action<ConsumeContext>>();
        }

        public void Probe(ProbeContext context)
        {
            context.Add("provider", "structuremap");
        }

        public ISagaScopeContext<T> GetScope<T>(ConsumeContext<T> context)
            where T : class
        {
            if (context.TryGetPayload<IContainer>(out var existingContainer))
            {
                existingContainer.Inject<ConsumeContext>(context);

                return new ExistingSagaScopeContext<T>(context);
            }

            var nestedContainer = _container?.CreateNestedContainer(context) ?? _context?.CreateNestedContainer(context);
            try
            {
                var proxy = new ConsumeContextScope<T>(context, nestedContainer);

                foreach (Action<ConsumeContext> scopeAction in _scopeActions)
                    scopeAction(proxy);

                return new CreatedSagaScopeContext<IContainer, T>(nestedContainer, proxy);
            }
            catch
            {
                nestedContainer.Dispose();
                throw;
            }
        }

        public ISagaQueryScopeContext<TSaga, T> GetQueryScope<T>(SagaQueryConsumeContext<TSaga, T> context)
            where T : class
        {
            if (context.TryGetPayload<IContainer>(out var existingContainer))
            {
                existingContainer.Inject<ConsumeContext>(context);

                return new ExistingSagaQueryScopeContext<TSaga, T>(context);
            }

            var nestedContainer = _container?.CreateNestedContainer(context) ?? _context?.CreateNestedContainer(context);
            try
            {
                var proxy = new SagaQueryConsumeContextScope<TSaga, T>(context, context.Query, nestedContainer);

                foreach (Action<ConsumeContext> scopeAction in _scopeActions)
                    scopeAction(proxy);

                return new CreatedSagaQueryScopeContext<IContainer, TSaga, T>(nestedContainer, proxy);
            }
            catch
            {
                nestedContainer.Dispose();
                throw;
            }
        }

        public void AddScopeAction(Action<ConsumeContext> action)
        {
            _scopeActions.Add(action);
        }
    }
}