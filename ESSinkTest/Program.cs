using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.Serialization;
using Serilog;
using Serilog.Sinks.ElasticSearch;

namespace ESSinkTest
{
    public class Program
    {
        static int Main(string[] args)
        {
            var p = new Program();

            // Select on of those methods to see the different issues
            p.NoSerializerOutputError();
            //p.WithSerializerOutputError();
            //p.WithSerializerButErrorOutputError();

            Console.WriteLine("Press a key to quit...");
            Console.ReadLine();

            return 0;
        }

        private void NoSerializerOutputError()
        {
            // This is a simple config with the ES sink. The only one that you can actually create with the AppSettings tricks (https://github.com/serilog/serilog/wiki/AppSettings)
            // The other extensions require more complex parameters so you cannot put them in appSettings.
            // With the mapping as specified here https://gist.github.com/mivano/9688328
            // if you output the below, you get two errors on the rolling sink, but only one error in ES. 

            Serilog.Debugging.SelfLog.Out = Console.Out;

            var loggerConfig = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .Enrich.WithMachineName()
               .WriteTo.ColoredConsole()
               .WriteTo.ElasticSearch("logstash-{0:yyyy.MM.dd}", new Uri("http://sv-ad-ap05:9200"));

            var logger = loggerConfig.CreateLogger();

            logger.Error(new Exception("test"), "Test exception. Should contain an embedded exception object.");   // This one is rejected by ES
            logger.Error("Test exception. Should not contain an embedded exception object.");                      // This one is included

            // The error you get in ES is as follows:
            /* 
             * org.elasticsearch.index.mapper.MapperParsingException: object mapping for [logevent] tried to parse as object, but got EOF, has a concrete value been provided to it?
	            at org.elasticsearch.index.mapper.object.ObjectMapper.parse(ObjectMapper.java:498)
	            at org.elasticsearch.index.mapper.DocumentMapper.parse(DocumentMapper.java:541)
	            at org.elasticsearch.index.mapper.DocumentMapper.parse(DocumentMapper.java:490)
	            at org.elasticsearch.index.shard.service.InternalIndexShard.prepareCreate(InternalIndexShard.java:392)
	            at org.elasticsearch.action.bulk.TransportShardBulkAction.shardIndexOperation(TransportShardBulkAction.java:444)
	            at org.elasticsearch.action.bulk.TransportShardBulkAction.shardOperationOnPrimary(TransportShardBulkAction.java:150)
	            at org.elasticsearch.action.support.replication.TransportShardReplicationOperationAction$AsyncShardOperationAction.performOnPrimary(TransportShardReplicationOperationAction.java:511)
	            at org.elasticsearch.action.support.replication.TransportShardReplicationOperationAction$AsyncShardOperationAction$1.run(TransportShardReplicationOperationAction.java:419)
	            at java.util.concurrent.ThreadPoolExecutor.runWorker(Unknown Source)
	            at java.util.concurrent.ThreadPoolExecutor$Worker.run(Unknown Source)
	            at java.lang.Thread.run(Unknown Source)
             * 
             */

            // It makes perfect sense, the toString is now called on the exception object because there is no serializer.
            // ES excepts however an object

        }

        private void WithSerializerOutputError()
        {
            // This is a config with the ES sink using the ElasticsearchSinkOptions. You can only do this from code.            
            // Using the same mapping as specified here https://gist.github.com/mivano/9688328

            Serilog.Debugging.SelfLog.Out = Console.Out;

            // if you output the below, you get two errors on the rolling sink and two error in ES. 

            var loggerConfig = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .Enrich.WithMachineName()
               .WriteTo.ColoredConsole()
               .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://sv-ad-ap05:9200"))
               {
                   Serializer = new ElasticsearchDefaultSerializer()
               });

            var logger = loggerConfig.CreateLogger();

            logger.Error(new Exception("test"), "Test exception. Should contain an embedded exception object.");   // 
            logger.Error("Test exception. Should not contain an embedded exception object.");                      // 

        }

        private void WithSerializerButErrorOutputError()
        {
            // This is a config with the ES sink using the ElasticsearchSinkOptions. You can only do this from code.            
            // Using the same mapping as specified here https://gist.github.com/mivano/9688328

            Serilog.Debugging.SelfLog.Out = Console.Out;

            // if you output the below, you get on the selflog an exception
            // Exception while emitting periodic batch from Serilog.Sinks.ElasticSearch.ElasticsearchSink: System.InvalidOperationException: 
            // Method may only be called on a Type for which Type.IsGenericParameter is true.

            var loggerConfig = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .Enrich.WithMachineName()
               .WriteTo.ColoredConsole()
               .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://sv-ad-ap05:9200"))
               {
                   Serializer = new ElasticsearchDefaultSerializer()
               });

            var logger = loggerConfig.CreateLogger();

            try
            {
                throw new TestException(String.Format("{0} ({1})", 404, "Not Found"), 404);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "TestException for {Name}", "testname");     // This will throw an error on the selflog.
            }


        }
    }

    [Serializable]
    public class ServiceException : Exception, ISerializable
    {


        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class.
        /// </summary>
        public ServiceException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ServiceException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="info"/> parameter is null.
        /// </exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">
        /// The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0).
        /// </exception>
        protected ServiceException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        { }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="System.Runtime.Serialization.SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="System.ArgumentNullException">
        /// The <paramref name="info"/> parameter is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <PermissionSet>
        /// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*"/>
        /// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter"/>
        /// </PermissionSet>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

    public class TestException : ServiceException
    {
        public int HttpStatusCode { get; set; }
        public TestException(String argument)
            : base(argument)
        { }

        public TestException(String argument, int httpStatusCode)
            : base(argument)
        { this.HttpStatusCode = httpStatusCode; }

        public TestException(String argument, Exception innerException)
            : base(argument, innerException)
        { }
    }

}
