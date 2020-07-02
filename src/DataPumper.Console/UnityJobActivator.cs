using Hangfire;
using Microsoft.Practices.Unity;
using System;

namespace DataPumper.Console
{
    internal class UnityJobActivator : JobActivator
    {
        private IUnityContainer container;

        public UnityJobActivator(IUnityContainer container)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            this.container = container;
        }

        /// <inheritdoc />
        public override object ActivateJob(Type jobType)
        {
            return this.container.Resolve(jobType);
        }

        public override JobActivatorScope BeginScope(JobActivatorContext context)
        {
            return new UnityScope(container.CreateChildContainer());
        }

        class UnityScope : JobActivatorScope
        {
            private readonly IUnityContainer container;

            public UnityScope(IUnityContainer container)
            {
                this.container = container;
            }

            public override object Resolve(Type type)
            {
                return container.Resolve(type);
            }

            public override void DisposeScope()
            {
                container.Dispose();
            }
        }
    }
}