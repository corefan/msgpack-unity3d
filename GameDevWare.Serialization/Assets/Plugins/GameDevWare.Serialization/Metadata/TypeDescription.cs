﻿/* 
Copyright (c) 2016 Denis Zykov, GameDevWare.com

https://www.assetstore.unity3d.com/#!/content/56706

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Serialization.Json.Exceptions;

namespace Serialization.Json.Metadata
{
	internal class TypeDescription : MemberDescription
	{
		private readonly Type objectType;
		private readonly ReadOnlyCollection<DataMemberDescription> members;
		private readonly Dictionary<string, DataMemberDescription> membersByName;

		public Type ObjectType { get { return this.objectType; } }
		public ReadOnlyCollection<DataMemberDescription> Members { get { return this.members; } }

		public TypeDescription(Type objectType)
			: base(objectType)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");

			this.objectType = objectType;
			var members = FindMembers(objectType);

			this.members = members.AsReadOnly();
			this.membersByName = members.ToDictionary(m => m.Name);
		}

		private static List<DataMemberDescription> FindMembers(Type objectType)
		{
			if (objectType == null) throw new ArgumentNullException("objectType");

			var members = new List<DataMemberDescription>();
			var memberNames = new HashSet<string>();

			var isOptIn = objectType.GetCustomAttributes(false).Any(a => a.GetType().Name == DATA_CONTRACT_ATTRIBUTE_NAME);
			var publicProperties =
				objectType.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
			var publicFields = objectType.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);

			foreach (var member in publicProperties.Cast<MemberInfo>().Concat(publicFields.Cast<MemberInfo>()))
			{
				if (member is PropertyInfo && (member as PropertyInfo).GetIndexParameters().Length != 0)
					continue;

				var dataMemberAttribute =
					member.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == DATA_MEMBER_ATTRIBUTE_NAME);
				var ignoreMemberAttribute =
					member.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == IGNORE_DATA_MEMBER_ATTRIBUTE_NAME);

				if (isOptIn && dataMemberAttribute == null)
					continue;
				else if (!isOptIn && ignoreMemberAttribute != null)
					continue;

				var dataMember = default(DataMemberDescription);
				if (member is PropertyInfo) dataMember = new PropertyDescription(member as PropertyInfo);
				else if (member is FieldInfo) dataMember = new FieldDescription(member as FieldInfo);
				else throw new InvalidOperationException("Unknown member type. Should be PropertyInfo or FieldInfo.");

				if (string.IsNullOrEmpty(dataMember.Name))
					throw new TypeContractViolation(objectType, "has no members with empty name");

				if (memberNames.Contains(dataMember.Name))
					throw new TypeContractViolation(objectType, string.Format("has no duplicate member's name ('{0}.{1}' and '{2}.{1}')", members.First(m => m.Name == dataMember.Name).Member.DeclaringType.Name, dataMember.Name, objectType.Name));

				members.Add(dataMember);
			}

			return members;
		}

		public bool TryGetMember(string name, out DataMemberDescription member)
		{
			return this.membersByName.TryGetValue(name, out member);
		}

		public override string ToString()
		{
			return this.objectType.ToString();
		}
	}
}