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

namespace cnatural.helpers {

	public abstract class UserDataContainer {
		private Object userData;
	
		protected UserDataContainer() {
		}

		public void addUserData(Object obj) {
			if (obj == null) {
				throw new IllegalArgumentException("obj");
			}
			if (this.userData == null) {
				if (obj instanceof Object[]) {
					this.userData = new Object[] { obj };
				} else {
					this.userData = obj;
				}
			} else if (this.userData instanceof Object[]) {
				var data = (Object[])this.userData;
				int i = 0;
				while (i < sizeof(data) && data[i] != null) {
					i++;
				}
				if (i == sizeof(data)) {
					data = resize(data);
					this.userData = data;
				}
				data[i] = obj;
			} else {
				this.userData = new Object[] { this.userData, obj };
			}
		}

		public final void addOrReplaceUserData(Object obj) {
			if (obj == null) {
				throw new IllegalArgumentException("obj");
			}
			if (this.userData == null) {
				if (obj instanceof Object[]) {
					this.userData = new Object[] { obj };
				} else {
					this.userData = obj;
				}
			} else if (this.userData instanceof Object[]) {
				var data = (Object[])this.userData;
				int i = 0;
				while (i < sizeof(data)) {
					var t = data[i];
					if (t == null || obj.getClass().isInstance(t)) {
						break;
					}
					i++;
				}
				if (i == sizeof(data)) {
					data = resize(data);
					this.userData = data;
				}
				data[i] = obj;
			} else if (obj.getClass().isInstance(this.userData)) {
				this.userData = obj;
			} else {
				this.userData = new Object[] { this.userData, obj };
			}
		}
		
		public T getUserData<T>(Class<T> c) {
			if (this.userData != null) {
				if (this.userData instanceof Object[]) {
					var dataList = (Object[])this.userData;
					foreach (var obj in dataList) {
						if (obj == null) {
							break;
						}
						#pragma warning disable 270
						T foundData = (c.isInstance(obj)) ? (T)obj : null;
						#pragma warning restore
						if (foundData != null) {
							return foundData;
						}
					}
				} else {
					#pragma warning disable 270
					return (c.isInstance(this.userData)) ? (T)this.userData : null;
					#pragma warning restore
				}
			}
			return null;
		}
		
		public Iterable<T> getAllUserData<T>(Class<T> c) {
			if (this.userData != null) {
				if (this.userData instanceof Object[]) {
					var dataList = (Object[])this.userData;
					foreach (Object obj in dataList) {
						if (obj == null) {
							break;
						}
						if (c.isInstance(obj)) {
							#pragma warning disable 270
							yield return (T)obj;
							#pragma warning restore
						}
					}
				} else if (c.isInstance(this.userData)) {
					#pragma warning disable 270
					yield return (T)this.userData;
					#pragma warning restore
				}
			}
		}
		
		public void removeUserData(Class<?> c) {
			if (this.userData != null) {
				if (this.userData instanceof Object[]) {
					var data = (Object[])this.userData;
					int i = 0;
					int n = 0;
					while (i < sizeof(data)) {
						// Put all the object that are not T at the beginning of the list
						var o = data[i];
						if (o == null) {
							break;
						}
						if (!c.isInstance(o)) {
							data[n++] = o;
						}
						i++;
					}
					if (n == 0) {
						this.userData = null;
					} else {
						while (n < i) {
							data[n++] = null;
						}
					}
				} else if (c.isInstance(this.userData)) {
					this.userData = null;
				}
			}
		}

		private static Object[] resize(Object[] array) {
			var result = new Object[sizeof(array) * 2];
			System.arraycopy(array, 0, result, 0, sizeof(array));
			return result;
		}
	}
}
