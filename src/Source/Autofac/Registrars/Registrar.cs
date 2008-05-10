﻿// This software is part of the Autofac IoC container
// Copyright (c) 2007 - 2008 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Component;
using Autofac.Component.Tagged;

namespace Autofac.Registrars
{
	/// <summary>
	/// Base class for component registrars.
	/// </summary>
	public abstract class Registrar<TSyntax> : IRegistrar<TSyntax>, IModule
        where TSyntax : IRegistrar<TSyntax>
	{
		IList<Service> _services = new List<Service>();
		InstanceOwnership _ownership = InstanceOwnership.Container;
		InstanceScope _scope = InstanceScope.Singleton;
        IList<EventHandler<PreparingEventArgs>> _preparingHandlers = new List<EventHandler<PreparingEventArgs>>();
        IList<EventHandler<ActivatingEventArgs>> _activatingHandlers = new List<EventHandler<ActivatingEventArgs>>();
        IList<EventHandler<ActivatedEventArgs>> _activatedHandlers = new List<EventHandler<ActivatedEventArgs>>();
        IList<EventHandler<RegisteredEventArgs>> _registeredHandlers = new List<EventHandler<RegisteredEventArgs>>();
        IDictionary<string, object> _extendedProperties = new Dictionary<string, object>();
        RegistrationCreator _createRegistration = (descriptor, activator, scope, ownership) =>
            new Registration(descriptor, activator, scope, ownership);

        /// <summary>
        /// Returns this instance, correctly-typed.
        /// </summary>
        protected abstract TSyntax Syntax { get; }

        /// <summary>
        /// Apply the module to the container.
        /// </summary>
        /// <param name="container">Container to apply configuration to.</param>
        public abstract void Configure(IContainer container);

		#region IRegistrar Members

		/// <summary>
		/// Change the service associated with the registration.
		/// </summary>
		/// <typeparam name="TService">The service that the registration will expose.</typeparam>
		/// <returns>A registrar allowing registration to continue.</returns>
        public virtual TSyntax As<TService>()
		{
			return As(new[] { typeof(TService) });
		}

		/// <summary>
		/// Change the services associated with the registration.
		/// </summary>
		/// <typeparam name="TService1">The first service that the registration will expose.</typeparam>
		/// <typeparam name="TService2">The second service that the registration will expose.</typeparam>
		/// <returns>A registrar allowing registration to continue.</returns>
        public virtual TSyntax As<TService1, TService2>()
		{
            return As(new[] { typeof(TService1), typeof(TService2) });
		}

		/// <summary>
		/// Change the services associated with the registration.
		/// </summary>
		/// <typeparam name="TService1">The first service that the registration will expose.</typeparam>
		/// <typeparam name="TService2">The second service that the registration will expose.</typeparam>
		/// <typeparam name="TService3">The third service that the registration will expose.</typeparam>
		/// <returns>A registrar allowing registration to continue.</returns>
        public virtual TSyntax As<TService1, TService2, TService3>()
		{
			return As(new[] { typeof(TService1), typeof(TService2), typeof(TService3) });
		}

        /// <summary>
        /// Change the service associated with the registration.
        /// </summary>
        /// <param name="services">The services that the registration will expose.</param>
        /// <returns>
        /// A registrar allowing registration to continue.
        /// </returns>
        public virtual TSyntax As(params Type[] services)
        {
            Enforce.ArgumentNotNull(services, "services");
            AddServices(services.Select<Type, Service>(s => new TypedService(s)));
            return Syntax;
        }

		/// <summary>
		/// Change the ownership model associated with the registration.
		/// This determines when the instances are disposed and by whom.
		/// </summary>
		/// <param name="ownership">The ownership model to use.</param>
		/// <returns>
		/// A registrar allowing registration to continue.
		/// </returns>
        public virtual TSyntax WithOwnership(InstanceOwnership ownership)
		{
			Ownership = ownership;
            return Syntax;
		}
        
        /// <summary>
        /// The instance(s) will not be disposed when the container is disposed.
        /// </summary>
        public virtual TSyntax ExternallyOwned()
        {
      		return WithOwnership(InstanceOwnership.External);
        }
        
        /// <summary>
        /// The instance(s) will be disposed with the container.
        /// </summary>
        public virtual TSyntax OwnedByContainer()
        {
       		return WithOwnership(InstanceOwnership.Container);
        }
        
		/// <summary>
		/// Change the scope associated with the registration.
		/// This determines how instances are tracked and shared.
		/// </summary>
		/// <param name="scope">The scope model to use.</param>
		/// <returns>
		/// A registrar allowing registration to continue.
		/// </returns>
        public virtual TSyntax WithScope(InstanceScope scope)
		{
			Scope = scope;
            return Syntax;
		}
        
        /// <summary>
        /// An instance will be created every time one is requested.
        /// </summary>
        public virtual TSyntax FactoryScoped()
        {
       		return WithScope(InstanceScope.Factory);
        }
        
        /// <summary>
        /// An instance will be created once per container.
        /// </summary>
        /// <seealso cref="IContainer.CreateInnerContainer" />
        public virtual TSyntax ContainerScoped()
        {
       		return WithScope(InstanceScope.Container);
        }
        
        /// <summary>
        /// Only one instance will ever be created.
        /// </summary>
        public virtual TSyntax SingletonScoped()
        {
       		return WithScope(InstanceScope.Singleton);
        }
        
        /// <summary>
        /// Call the provided handler when activating an instance.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>A registrar allowing registration to continue.</returns>
        public virtual TSyntax OnRegistered(EventHandler<RegisteredEventArgs> handler)
        {
            Enforce.ArgumentNotNull(handler, "handler");
            _registeredHandlers.Add(handler);
            return Syntax;
        }

        /// <summary>
        /// Call the provided handler when preparing to activate an instance. OnPreparing
        /// is the place to interrupt of modify the parameters to the activation process.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>
        /// A registrar allowing registration to continue.
        /// </returns>
        public virtual TSyntax OnPreparing(EventHandler<PreparingEventArgs> handler)
        {
            Enforce.ArgumentNotNull(handler, "handler");
            _preparingHandlers.Add(handler);
            return Syntax;
        }

        /// <summary>
        /// Call the provided handler when activating an instance.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>A registrar allowing registration to continue.</returns>
        public virtual TSyntax OnActivating(EventHandler<ActivatingEventArgs> handler)
        {
            Enforce.ArgumentNotNull(handler, "handler");
            _activatingHandlers.Add(handler);
            return Syntax;
        }

        /// <summary>
        /// Call the provided handler when an instance is activated.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>A registrar allowing registration to continue.</returns>
        public virtual TSyntax OnActivated(EventHandler<ActivatedEventArgs> handler)
        {
            Enforce.ArgumentNotNull(handler, "handler");
            _activatedHandlers.Add(handler);
            return Syntax;
        }

        /// <summary>
        /// Associates an extended property with the registration. The property must not
        /// already exist.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A registrar allowing registration to continue.
        /// </returns>
        public virtual TSyntax WithExtendedProperty(string key, object value)
        {
            Enforce.ArgumentNotNull(key, "key");
            _extendedProperties.Add(key, value);
            return Syntax;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tag"></param>
        /// <returns></returns>
        public virtual TSyntax InContext<T>(T tag)
        {
        	var oldValue = RegistrationCreator;
            RegistrationCreator = (descriptor, activator, scope, ownership) =>
            	new TaggedRegistration<T>(tag, oldValue(descriptor, activator, scope, ownership));
            return Syntax;
        }

        /// <summary>
        /// Gets or sets the registration creator.
        /// </summary>
        /// <value>The registration creator.</value>
        public virtual RegistrationCreator RegistrationCreator
        {
            get
            {
                return _createRegistration;
            }
            set
            {
                _createRegistration = Enforce.ArgumentNotNull(value, "value");
            }
        }

		#endregion


		/// <summary>
		/// The services exposed by this registration.
		/// </summary>
		protected virtual IEnumerable<Service> Services
		{
			get
			{
				return _services;
			}
		}

        /// <summary>
        /// Add a service to be exposed by the component.
        /// </summary>
        /// <param name="service"></param>
        protected virtual void AddService(Service service)
        {
            Enforce.ArgumentNotNull(service, "service");
            _services.Add(service);
        }

        /// <summary>
        /// Add many services to be exposed by the component.
        /// </summary>
        protected virtual void AddServices(IEnumerable<Service> services)
        {
            Enforce.ArgumentNotNull(services, "services");
            foreach (var service in services)
                AddService(service);
        }

		/// <summary>
		/// The instance scope used by this registration.
		/// </summary>
        protected virtual InstanceScope Scope
		{
			get
			{
				return _scope;
			}
			set
			{
				_scope = value;
			}
		}

		/// <summary>
		/// The instance ownership used by this registration.
		/// </summary>
        protected virtual InstanceOwnership Ownership
		{
			get
			{
				return _ownership;
			}
			set
			{
				_ownership = value;
			}
		}

        /// <summary>
        /// The handlers for the Preparing event used by this registration.
        /// </summary>
        protected virtual IEnumerable<EventHandler<PreparingEventArgs>> PreparingHandlers
        {
            get
            {
                return _preparingHandlers;
            }
        }

        /// <summary>
        /// The handlers for the Activating event used by this registration.
        /// </summary>
        protected virtual IEnumerable<EventHandler<ActivatingEventArgs>> ActivatingHandlers
        {
            get
            {
                return _activatingHandlers;
            }
        }

        /// <summary>
        /// The handlers for the Activated event used by this registration.
        /// </summary>
        protected virtual IEnumerable<EventHandler<ActivatedEventArgs>> ActivatedHandlers
        {
            get
            {
                return _activatedHandlers;
            }
        }


        /// <summary>
        /// Fires the registered event.
        /// </summary>
        /// <param name="e">The <see cref="Autofac.Registrars.RegisteredEventArgs"/> instance containing the event data.</param>
        protected virtual void FireRegistered(RegisteredEventArgs e)
        {
            Enforce.ArgumentNotNull(e, "e");
            foreach (EventHandler<RegisteredEventArgs> handler in _registeredHandlers)
                handler(this, e);
        }

        /// <summary>
        /// Gets the extended properties.
        /// </summary>
        /// <value>The extended properties.</value>
        protected virtual IDictionary<string, object> ExtendedProperties
        {
            get
            {
                return _extendedProperties;
            }
        }

        /// <summary>
        /// Sets up the registration with events, registers it in the container, and fires
        /// the Registered event.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="cr">The cr.</param>
        protected virtual void RegisterComponent(IContainer container, IComponentRegistration cr)
        {
            Enforce.ArgumentNotNull(container, "container");
            Enforce.ArgumentNotNull(cr, "cr");

            foreach (var preparingHandler in PreparingHandlers)
                cr.Preparing += preparingHandler;

            foreach (var activatingHandler in ActivatingHandlers)
                cr.Activating += activatingHandler;

            foreach (var activatedHandler in ActivatedHandlers)
                cr.Activated += activatedHandler;

            container.RegisterComponent(cr);

            FireRegistered(new RegisteredEventArgs() { Container = container, Registration = cr });
        }
    }
}