﻿using AmazedSaint.Elastic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmazedSaint.Elastic.Lib;
using System.Diagnostics;

namespace AmazedSaint.Elastic.Tests
{
    
    
    /// <summary>
    ///This is a test class for DynamicExtensionsTest and is intended
    ///to contain all DynamicExtensionsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DynamicExtensionsTest
    {

        /// <summary>
        /// A private helper method
        /// </summary>
        /// <returns></returns>
        private  dynamic CreateStoreObject()
        {
            dynamic store = new ElasticObject("Store");
            store.Name = "Acme Store";
            store.Location.Address = "West Avenue, Heaven Street Road, LA";
            store.Products.Count = 2;

            store.Owner.FirstName = "Jack";
            store.Owner.SecondName = "Jack";

            //try to set the internal content for an element
            store.Owner <<= "this is some internal content for owner";

            //Add a new product
            var p1 = store.Products.Product();
            p1.Name = "Acme Floor Cleaner";
            p1.Price = 20;

            //Add another product
            var p2 = store.Products.Product();
            p2.Name = "Acme Bun";
            p2.Price = 22;

            return store;

        }


        [Description("Test to verify the Object creation"), TestMethod()]
        public void Check_The_Object_Get_Created() 
        {
            CreateStoreObject();
        }



        [Description("Test to verify the Xml creation using '>' or format converter operator"),TestMethod()]
        public void Check_The_Object_Create_And_Consume_Xml()
        {
            var store = CreateStoreObject();
            XElement el = store > FormatType.Xml;
            dynamic storeClone = el.ToElastic();
            XElement elCopy = storeClone > FormatType.Xml;
            Assert.AreEqual(el.ToString(), elCopy.ToString());
        }


        [Description("Test to verify the indexer returns all children when passing a null"),TestMethod()]
        public void Check_The_Object_Indexer_Return_Children_For_Null()
        {
            var store = CreateStoreObject();
            var products = store.Products[null];

            Assert.AreEqual(2, products.Count);
        }

        [Description("Test to verify the indexer works nice when passing a named item"), TestMethod()]
        public void Check_The_Object_Return_Named_Children()
        {
            var store = CreateStoreObject();
            var owner = store["Owner"] as IEnumerable<dynamic>;
            Assert.AreEqual(owner.First().FirstName, "Jack");
        }


        [Description("Test to verify '<<' operator to add elements"), TestMethod()]
        public void Check_The_LeftShift_Operator_Can_Add_An_Element_By_Name()
        {
            dynamic myobj = new ElasticObject("MyObject");

            for (int i = 0; i < 10; i++)
            {
                var newItem = myobj << "Item";
                newItem.CountNumber = i;
            }

            Assert.AreEqual(10, myobj["Item"].Count);

        }

        [Description("Test to verify '<' operator to add attributes"), TestMethod()]
        public void Check_The_LessThan_Operator_Can_Add_An_Attribute_By_Name()
        {
            dynamic myobj = new ElasticObject("MyObject");

            for (int i = 0; i < 10; i++)
            {
                var newItem = myobj < "Attrib" + i;
                newItem <<= "somevalue";
            }

            //Few random checks

            Assert.AreEqual(myobj.Attrib1, "somevalue");
            Assert.AreEqual(myobj.Attrib8, "somevalue");
        }


        [Description("Test to verify integer in Indexer"), TestMethod()]
        public void Check_The_Object_Indexer_Can_Accept_Integers()
        {
            dynamic myobj = new ElasticObject("MyObject");

            for (int i = 0; i < 10; i++)
            {
                var newItem = myobj << "Item";
                newItem.CountNumber = i;
            }

            //Check the 3rd and 9th items
            Assert.AreEqual(3, myobj[3].CountNumber);
            Assert.AreEqual(9, myobj[9].CountNumber);
        }


        [Description("Test to verify '<<' and '<' operator to add elements and attributes"), TestMethod()]
        public void Check_The_Element_And_Attributes_Can_Be_Added_Using_Operators()
        {
            dynamic myobj = new ElasticObject("MyObject");

            var attrib1= (myobj << "element") < "attribute1";
            attrib1 <<= "hello1";
            var attrib2 = (myobj << "element") < "attribute1";
            attrib2 <<= "hello2";

            //first element
            Assert.AreEqual(myobj[0].attribute1, "hello1");

            //second element
            Assert.AreEqual(myobj[1].attribute1, "hello2");

        }


        [Description("Test to verify Indexer can accept a filter delegate"), TestMethod()]
        public void Check_The_Indexer_Take_A_Delegate()
        {
            dynamic myobj = new ElasticObject("MyObject");

            for (int i = 0; i < 10; i++)
            {
                var newItem = myobj << "Item";
                newItem.CountNumber = i;
            }

            var filter=new Func<dynamic,bool>((obj)=>obj.CountNumber>5);
            var result = myobj[filter];

            //4 items remains above 5
            Assert.AreEqual(4, result.Count);
        }

        [TestMethod]
        [Description("Test to verify traversing")]
        public void Check_Nested_Objects_Traversing()
        {
            dynamic model = new ElasticObject();
            var c1 = model.@class();
            c1.name = "Class1";
            var p = c1.property();
            p.name = "Property1";
            p.type = "string";
            p = c1.property();
            p.name = "Property1";
            p.type = "string";

            var c2 = model.@class();
            p = c2.property();
            c2.name = "Class2";
            p.name = "Property1";
            p.type = "string";
            p = c2.property();
            p.name = "Property1";
            p.type = "string";

            Assert.AreEqual(model["class"].Count,2);
            Assert.AreEqual(model["class"][0]["property"].Count,2);

        }


        [TestMethod]
        [Description("Test to verify property passing via set")]
        public void Check_Named_Parameters()
        {
            dynamic model = new ElasticObject();
            var c1 = model.@class(new { name = "Class1" });
            c1.property(new { name = "Property1", type = "string" });
            c1.property(new { name = "Property2", type = "string" });
            var c2 = model.@class(new { name = "Class2" });
            c2.property(new { name = "Property1", type = "string" });
            c2.property(new { name = "Property2", type = "string" });

            Assert.AreEqual(model["class"].Count, 2);
            Assert.AreEqual(model["class"][0]["property"].Count, 2);

        }

        private XElement CreateTestXElementWithNamespaces()
        {
            string data = @"<Store Name=""Acme Store"" xmlns=""http://example.com/store/v1-0"">
<Location Address= ""West Avenue"" />
<Products Count=""1""  xmlns=""http://example.com/products/v1-0"">
    <Product Name =""Acme Bun"" />
</Products>
<Owner>Content</Owner>
</Store>";

            return XElement.Parse(data);
        }

        [TestMethod]
        [Description("Test to verify xmlnamespace support")]
        public void Check_Namespace_Support()
        {
            var el = CreateTestXElementWithNamespaces();
            var elastic = el.ToElastic();

            XElement testcase = elastic > FormatType.Xml;

            // ToString() should throw excepton if no Namespace support
            var data = testcase.ToString();
        }

    }
}
