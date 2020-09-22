using Hangfire;
using Microsoft.Practices.Unity;
using System;

namespace DataPumper.Console
{
    internal class UnityJobActivator : JobActivator
    {
        private readonly IUnityContainer _container;

        public UnityJobActivator(IUnityContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <inheritdoc />
        public override object ActivateJob(Type jobType)
        {
            return _container.Resolve(jobType);
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            return new UnityScope(_container.CreateChildContainer());
        }

        private class UnityScope : JobActivatorScope
        {
            private readonly IUnityContainer _container;

            public UnityScope(IUnityContainer container)
            {
                _container = container;
            }

            public override object Resolve(Type type)
            {
                return _container.Resolve(type);
            }

            public override void DisposeScope()
            {
                _container.Dispose();
            }
        }
    }
}