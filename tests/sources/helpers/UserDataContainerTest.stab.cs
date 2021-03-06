/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */
using java.lang;
using org.junit;
using stab.query;

namespace cnatural.helpers.test {
	
	public class UserDataContainerTest {
		[Test]
		public void testObject() {
			var c = new Container();
			
			var obj = new Object();
			c.addUserData(obj);
			
			Assert.assertEquals(obj, c.getUserData(typeof(Object)));
		}

		[Test]
		public void testDerivedClasses() {
			var c = new Container();
			
			var obj = new Object();
			var str = "abc";
			c.addUserData(obj);
			c.addUserData(str);
			
			Assert.assertEquals(obj, c.getUserData(typeof(Object)));
			Assert.assertEquals("abc", c.getUserData(typeof(String)));
		}

		[Test]
		public void testDerivedClasses2() {
			var c = new Container();
			
			var obj = new Object();
			var str = "abc";
			c.addUserData(str);
			c.addUserData(obj);
			
			Assert.assertEquals(str, c.getUserData(typeof(Object)));
			Assert.assertEquals("abc", c.getUserData(typeof(String)));
		}
		
		[Test]
		public void testArrays() {
			var c = new Container();
			
			var obj = new Object();
			var str = "abc";
			var arr = new String[] { "a", "b", "c" };
			c.addUserData(obj);
			c.addUserData(str);
			c.addUserData(arr);
			
			Assert.assertEquals(obj, c.getUserData(typeof(Object)));
			Assert.assertEquals("abc", c.getUserData(typeof(String)));
			Assert.assertEquals((Object)arr, c.getUserData(typeof(String[])));
		}
		
		[Test]
		public void testArrays2() {
			var c = new Container();
			
			var obj = new Object();
			var str = "abc";
			var arr = new Object[] { "a", "b", "c" };
			c.addUserData(arr);
			c.addUserData(obj);
			c.addUserData(str);
			
			Assert.assertEquals(arr, c.getUserData(typeof(Object)));
			Assert.assertEquals("abc", c.getUserData(typeof(String)));
			Assert.assertEquals((Object)arr, c.getUserData(typeof(Object[])));
		}
		
		[Test]
		public void testRemoveObject() {
			var c = new Container();

			var obj = new Object();
			c.addUserData(obj);
			c.removeUserData(typeof(Object));
			
			Assert.assertEquals(null, c.getUserData(typeof(Object)));
		}
		
		[Test]
		public void testRemoveDerivedClasses() {
			var c = new Container();
			
			var obj = new Object();
			var str = "abc";
			c.addUserData(obj);
			c.addUserData(str);
			c.removeUserData(typeof(Object));
			
			Assert.assertEquals(null, c.getUserData(typeof(Object)));
			Assert.assertEquals(null, c.getUserData(typeof(String)));
		}

		[Test]
		public void testGetAllUserData() {
			var c = new Container();
			
			var strs = new String[] { "str1", "str2", "str3", "str4" };

			foreach (var s in strs) {
				c.addUserData(s);
			}
			
			Assert.assertTrue(c.getAllUserData(typeof(String)).sequenceEqual(Query.asIterable(strs)));
		}
		
		private class Container : UserDataContainer {
		}
	}
}
