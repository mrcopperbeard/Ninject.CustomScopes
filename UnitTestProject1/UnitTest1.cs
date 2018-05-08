using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject;
using Ninject.Modules;
using Ninject.Syntax;
using Sphere.Common.Logging;
using Sphere.Shared.Core;

namespace UnitTestProject1
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			var kernel = new StandardKernel(new Module());
		}
	}

	internal class Module : NinjectModule
	{
		/// <summary>
		/// Loads the module into the kernel.
		/// </summary>
		public override void Load()
		{
			// ReSharper disable once AssignNullToNotNullAttribute
			Bind<ILogger>().ToMethod(context => context.Kernel.Get<ILoggerFactory>().GetLogger(context.Request.Target.Member.DeclaringType));
			Bind<ILoggerFactory>().To<NLogLoggerFactory>();
			Bind<ILoggerContext>().To<CurrentUserLoggerContext>();

			Bind<IInstance>().To<Instance>();
			Bind<IFactory>().To<Factory>();
		}
	}

	internal class EntryPoint
	{
		private ILogger _logger;

		private readonly IFactory _factory;

		public EntryPoint(ILogger logger, IFactory factory)
		{
			_logger = logger;
			_factory = factory;
		}

		public void Foo()
		{
			_logger.Info("Start");

			var instances = new List<IInstance>
			{
				_factory.Create(),
				_factory.Create(),
				_factory.Create(),
			};

			foreach (var instance in instances)
			{
				instance.Foo();
			}

			_logger.Info("Finish");
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
		void Foo();
	}

	internal class Instance : IInstance
	{
		private readonly ILogger _logger;

		public Instance(ILogger logger)
		{
			_logger = logger;
		}

		public void Foo()
		{
			_logger.Info("First from instance");
			_logger.WithProperty("Key", "Value").Info("Second from instance");
			_logger.Error("Third from instance");
		}
	}
}
