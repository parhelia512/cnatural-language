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
using java.text;
using java.util;

namespace cnatural.helpers {

	public class ResourceManager {
		private Map<Locale, ResourceBundle> resourceBundles;
		private Class<?> targetPackageClass;
		private String shortName;

		public ResourceManager(Class<?> c, String shortName) {
			this.resourceBundles = new HashMap<Locale, ResourceBundle>();
			this.targetPackageClass = c;
			this.shortName = shortName;
		}

		public String getMessage(Locale locale, String key, params Object[] args) {
			var rb = getResourceBundle(locale);
			if (rb != null) {
				try {
					return MessageFormat.format(rb.getString(key), args);
				} catch {
				}
			}
			return key;
		}

		private synchronized ResourceBundle getResourceBundle(Locale locale) {
			var result = resourceBundles.get(locale);
			if (result == null) {
				result = loadResources(locale);
				if (result != null) {
					resourceBundles.put(locale, result);
				}
			}
			return result;
		}

		private ResourceBundle loadResources(Locale locale) {
			try {
				return ResourceBundle.getBundle(targetPackageClass.getPackage().getName() + "." + shortName, locale);
			} catch (MissingResourceException e) {
				return null;
			}
		}
	}
}
