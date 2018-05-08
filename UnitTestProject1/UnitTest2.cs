using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.ContextPreservation;
using Ninject.Modules;
using Ninject.Syntax;
using NUnit.Framework;

namespace UnitTestProject1
{
	[TestFixture]
	public class UnitTest2
	{
		[Test]
		public void TestMethod1()
		{
			var kernel = new StandardKernel(new NinjectSettings
			{
				LoadExtensions = false
			});

			kernel.Load(new Module());
			kernel.Load(new ContextPreservationModule());

			var point1 = kernel.Get<EntryPoint>();
			var point2 = kernel.Get<EntryPoint>();

			point1.Foo();
			point1.ThreadFoo();

			point1.Instance.Guid.Should().NotBe(point2.Instance.Guid);
		}
	}

	internal class Module : NinjectModule
	{
		/// <summary>
		/// Loads the module into the kernel.
		/// </summary>
		public override void Load()
		{
			Bind<EntryPoint>().ToSelf();
			Bind<IInstance>().To<Instance>().InScope(GetEntryPoint);
			Bind<IFactory>().To<Factory>();
		}

		private static object GetEntryPoint(IContext context)
		{
			var service = context.Request.Service;
			if (service.GetCustomAttributes(typeof(EntryPointAttribute), true).Any())
			{
				var test = context.Kernel.Get(typeof(ScopeObject));

				return test; // TODO: Instance of entry point
			}

			if (context.Request.Depth > 0)
			{
				return GetEntryPoint(context.Request.ParentContext);
			}

			throw new ActivationException("Invalid Entry Point binding.");
		}
	}

	public class ScopeObject
	{
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class EntryPointAttribute : Attribute
	{
	}

	[EntryPoint]
	internal class EntryPoint
	{
		public ScopeObject ScopeObject { get; }

		private readonly IFactory _factory;

		public readonly IInstance Instance;

		public EntryPoint(ScopeObject scopeObject, IInstance instance, IFactory factory)
		{
			_factory = factory;
			Instance = instance;
			ScopeObject = scopeObject;
		}

		public void Foo()
		{
			var instances = new List<IInstance>
			{
				_factory.Create(),
				_factory.Create(),
				_factory.Create(),
			};

			instances.Select(i => i.Guid).Should().AllBeEquivalentTo(Instance.Guid);
		}

		public void ThreadFoo()
		{
			var thread = new Thread(Foo);
			thread.Start();
		}
	}

	internal interface IFactory
	{
		IInstance Create();
	}

	internal class Factory : IFactory
	{
		private readonly IResolutionRoot _root;

		public Factory(IResolutionRoot root)
		{
			_root = root;
		}

		public IInstance Create()
		{
			return _root.Get<IInstance>();
		}
	}

	internal interface IInstance
	{
		Guid Guid { get; }
	}

	internal class Instance : IInstance
	{
		public Guid Guid { get; set; }

		public Instance()
		{
			Guid = Guid.NewGuid();
		}
	}
}
