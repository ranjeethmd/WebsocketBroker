using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using WebsocketBroker.Core.IO;

namespace WebsocketBroker.Test
{
    [TestClass]
    public class TopicTest
    {
        private Topic _topic;

        [TestInitialize()]
        public void Initialize()
        {
            var name = "Test";
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _topic = new Topic(name,path);

        }
        
        [TestMethod]
        public void TestMappedFileCreation()
        {
            
            try
            {
                _topic.CreatePartition();
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            
        }

        [TestMethod]
        public void TestTopicAppend()
        {
            
            var bytes1 = Encoding.UTF8.GetBytes("Hello world!");
            var bytes2 = Encoding.UTF8.GetBytes("You are doing great!");
            try
            {
                _topic.CreatePartition();
                _topic.AppendData(bytes1);
                _topic.AppendData(bytes2);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message,ex.StackTrace);
            }

        }

        [TestMethod]
        public void TestTopicRead()
        {        
           
            try
            {
                _topic.CreatePartition();
                var bytes2 = _topic.ReadData(1);
                var bytes1 = _topic.ReadData(0);

                Assert.AreEqual("Hello world!", Encoding.UTF8.GetString(bytes1));
                Assert.AreEqual("You are doing great!", Encoding.UTF8.GetString(bytes2));
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message, ex.StackTrace);
            }

        }
    }
}
