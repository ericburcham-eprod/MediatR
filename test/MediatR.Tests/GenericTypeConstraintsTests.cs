namespace MediatR.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Shouldly;
    using StructureMap;
    using System.Threading.Tasks;
    using Xunit;

    public class GenericTypeConstraintsTests
    {
        public interface IGenericTypeRequestHandlerTestClass<TRequest> where TRequest : IRequest
        {
            Type[] Handle(TRequest request);
        }

        public abstract class GenericTypeRequestHandlerTestClass<TRequest> : IGenericTypeRequestHandlerTestClass<TRequest>
            where TRequest : IRequest
        {
            public bool IsIRequest { get; private set; }


            public bool IsIRequestT { get; private set; }

            public GenericTypeRequestHandlerTestClass()
            {
                IsIRequest = typeof(IRequest).IsAssignableFrom(typeof(TRequest));
                IsIRequestT = typeof(TRequest).GetInterfaces()
                                                   .Any(x => x.IsGenericType &&
                                                             x.GetGenericTypeDefinition() == typeof(IRequest<>));
            }

            public Type[] Handle(TRequest request)
            {
                return typeof(TRequest).GetInterfaces();
            }
        }

        public class GenericTypeConstraintPing : GenericTypeRequestHandlerTestClass<Ping>
        {

        }

        public class GenericTypeConstraintJing : GenericTypeRequestHandlerTestClass<Jing>
        {

        }

        public class Jing : IRequest
        {
            public string Message { get; set; }
        }

        public class JingHandler : IRequestHandler<Jing>
        {
            public void Handle(Jing message)
            {
                // empty handle
            }
        }

        public class Ping : IRequest<Pong>
        {
            public string Message { get; set; }
        }

        public class Pong
        {
            public string Message { get; set; }
        }

        public class PingHandler : IRequestHandler<Ping, Pong>
        {
            public Pong Handle(Ping message)
            {
                return new Pong { Message = message.Message + " Pong" };
            }
        }

        private readonly IMediator _mediator;

        public GenericTypeConstraintsTests()
        {
            var container = new Container(cfg =>
            {
                cfg.Scan(scanner =>
                {
                    scanner.AssemblyContainingType(typeof(GenericTypeConstraintsTests));
                    scanner.IncludeNamespaceContainingType<Ping>();
                    scanner.IncludeNamespaceContainingType<Jing>();
                    scanner.WithDefaultConventions();
                    scanner.AddAllTypesOf(typeof(IRequestHandler<,>));
                    scanner.AddAllTypesOf(typeof(IRequestHandler<>));
                });
                cfg.For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => t => ctx.GetInstance(t));
                cfg.For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => t => ctx.GetAllInstances(t));
                cfg.For<IMediator>().Use<Mediator>();
            });

            _mediator = container.GetInstance<IMediator>();
        }

        [Fact]
        public async Task Should_Resolve_Void_Return_Request()
        {
            // Create Request
            var jing = new Jing { Message = "Jing" };

            // Test mediator still works sending request
            await _mediator.Send(jing);

            // Create new instance of type constrained class
            var genericTypeConstraintsVoidReturn = new  GenericTypeConstraintJing();

            // Assert it is of type IRequest but not IRequest<T>
            Assert.True(genericTypeConstraintsVoidReturn.IsIRequest);
            Assert.False(genericTypeConstraintsVoidReturn.IsIRequestT);

            // Verify it is of IRequest
            genericTypeConstraintsVoidReturn.Handle(jing)
                .Select(x => x.ShouldBeOfType<IRequest>());
        }

        [Fact]
        public async Task Should_Resolve_Response_Return_Request()
        {
            // Create Request
            var ping = new Ping { Message = "Ping" };

            // Test mediator still works sending request and gets response
            var pingResponse = await _mediator.Send(ping);
            pingResponse.Message.ShouldBe("Ping Pong");

            // Create new instance of type constrained class
            var genericTypeConstraintsResponseReturn = new GenericTypeConstraintPing();

            // Assert it is of type IRequest and IRequest<T>
            Assert.True(genericTypeConstraintsResponseReturn.IsIRequest);
            Assert.True(genericTypeConstraintsResponseReturn.IsIRequestT);

            // Verify it is of IRequest<Pong>
            genericTypeConstraintsResponseReturn.Handle(ping)
                .Select(x => x.ShouldBeOfType<IRequest<Pong>>())
                .Select(x => x.ShouldBeOfType<IRequest>());
        }
    }
}
