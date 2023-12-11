// Current Nethereum Sdk is having issues with EIP712, so we have to use a custom implementation (based on them)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

using Nethereum.ABI.ABIDeserialisation;
using Nethereum.ABI.EIP712;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nethereum.ABI.EIP712
{
    public class CustomEip712TypedDataEncoder
    {
        private readonly ABIEncode _abiEncode = new ABIEncode();
        public static CustomEip712TypedDataEncoder Current { get; } = new CustomEip712TypedDataEncoder();
        private readonly ParametersEncoder _parametersEncoder = new ParametersEncoder();

        // <summary>
        /// Encodes data according to EIP-712, it uses a predefined typed data schema and converts and encodes the provide the message value
        /// </summary>
        public byte[] EncodeTypedData<T, TDomain>(T message, TypedData<TDomain> typedData)
        {
            typedData.Message = MemberValueFactory.CreateFromMessage(message);
            typedData.EnsureDomainRawValuesAreInitialised();
            return EncodeTypedDataRaw(typedData);
        }

        /// <summary>
        /// Encodes data according to EIP-712.
        /// Infers types of message fields from <see cref="Nethereum.ABI.FunctionEncoding.Attributes.ParameterAttribute"/>. 
        /// For flat messages only, for complex messages with reference type fields use "EncodeTypedData(TypedData typedData).
        /// </summary>
        public byte[] EncodeTypedData<T, TDomain>(T data, TDomain domain, string primaryTypeName)
        {
            var typedData = GenerateTypedData(data, domain, primaryTypeName);

            return EncodeTypedData(typedData);
        }

        public byte[] EncodeTypedData(string json)
        {
            var typedDataRaw = CustomTypedDataRawJsonConversion.DeserialiseJsonToRawTypedData(json);
            return EncodeTypedDataRaw(typedDataRaw);
        }

        /// <summary>
        /// Encode typed data using a non standard json, which may not include the Domain type and uses a different key selector for message
        /// </summary>
        public byte[] EncodeTypedData<DomainType>(string json, string messageKeySelector = "message")
        {
            var typedDataRaw = CustomTypedDataRawJsonConversion.DeserialiseJsonToRawTypedData<DomainType>(json, messageKeySelector);
            return EncodeTypedDataRaw(typedDataRaw);
        }

        public byte[] EncodeAndHashTypedData<T, TDomain>(T message, TypedData<TDomain> typedData)
        {
            var encodedData = EncodeTypedData(message, typedData);
            return Sha3Keccack.Current.CalculateHash(encodedData);
        }

        public byte[] EncodeAndHashTypedData<TDomain>(TypedData<TDomain> typedData)
        {
            var encodedData = EncodeTypedData(typedData);
            return Sha3Keccack.Current.CalculateHash(encodedData);
        }

        /// <summary>
        /// Encodes data according to EIP-712.
        /// </summary>
        public byte[] EncodeTypedData<TDomain>(TypedData<TDomain> typedData)
        {
            typedData.EnsureDomainRawValuesAreInitialised();
            return EncodeTypedDataRaw(typedData);
        }

        public byte[] EncodeTypedDataRaw(TypedDataRaw typedData)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write("1901".HexToByteArray());
                writer.Write(HashStruct(typedData.Types, "EIP712Domain", typedData.DomainRawValues));
                writer.Write(HashStruct(typedData.Types, typedData.PrimaryType, typedData.Message));

                writer.Flush();
                var result = memoryStream.ToArray();
                return result;
            }
        }

        public byte[] HashDomainSeparator<TDomain>(TypedData<TDomain> typedData)
        {
            typedData.EnsureDomainRawValuesAreInitialised();
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write(HashStruct(typedData.Types, "EIP712Domain", typedData.DomainRawValues));
                writer.Flush();
                var result = memoryStream.ToArray();
                return result;
            }
        }

        public byte[] HashStruct<T>(T message, string primaryType, params Type[] types)
        {
            var memberDescriptions = MemberDescriptionFactory.GetTypesMemberDescription(types);
            var memberValue = MemberValueFactory.CreateFromMessage(message);
            return HashStruct(memberDescriptions, primaryType, memberValue);
        }

        public string GetEncodedType(string primaryType, params Type[] types)
        {
            var memberDescriptions = MemberDescriptionFactory.GetTypesMemberDescription(types);
            return EncodeType(memberDescriptions, primaryType);
        }

        public string GetEncodedTypeDomainSeparator<TDomain>(TypedData<TDomain> typedData)
        {
            typedData.EnsureDomainRawValuesAreInitialised();
            return EncodeType(typedData.Types, "EIP712Domain");
        }

        private byte[] HashStruct(IDictionary<string, MemberDescription[]> types, string primaryType, IEnumerable<MemberValue> message)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                var encodedType = EncodeType(types, primaryType);
                var typeHash = Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(encodedType));
                writer.Write(typeHash);

                EncodeData(writer, types, message);

                writer.Flush();
                return Sha3Keccack.Current.CalculateHash(memoryStream.ToArray());
            }
        }

        private static string EncodeType(IDictionary<string, MemberDescription[]> types, string typeName)
        {
            var encodedTypes = EncodeTypes(types, typeName);
            var encodedPrimaryType = encodedTypes.Single(x => x.Key == typeName);
            var encodedReferenceTypes = encodedTypes.Where(x => x.Key != typeName).OrderBy(x => x.Key).Select(x => x.Value);
            var fullyEncodedType = encodedPrimaryType.Value + string.Join(string.Empty, encodedReferenceTypes.ToArray());

            return fullyEncodedType;
        }

        private static IList<KeyValuePair<string, string>> EncodeTypes(IDictionary<string, MemberDescription[]> types, string currentTypeName)
        {
            var currentTypeMembers = types[currentTypeName];
            var currentTypeMembersEncoded = currentTypeMembers.Select(x => x.Type + " " + x.Name);
            var result = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(currentTypeName, currentTypeName + "(" + string.Join(",", currentTypeMembersEncoded.ToArray()) + ")")
            };

            result.AddRange(currentTypeMembers.Select(x => ConvertToElementType(x.Type)).Distinct().Where(IsReferenceType).SelectMany(x => EncodeTypes(types, x)));

            return result;
        }

        private static string ConvertToElementType(string type)
        {
            if (type.Contains("["))
            {
                return type.Substring(0, type.IndexOf("["));
            }
            return type;
        }

        internal static bool IsReferenceType(string typeName)
        {
            switch (typeName)
            {
                // TODO: specify more precise conditions
                case var bytes when new Regex("bytes\\d+").IsMatch(bytes):
                case var @uint when new Regex("uint\\d+").IsMatch(@uint):
                case var @int when new Regex("int\\d+").IsMatch(@int):
                case "bytes":
                case "string":
                case "bool":
                case "address":
                    return false;
                case var array when array.Contains("["):
                    return false;
                default:
                    return true;
            }
        }

        private void EncodeData(BinaryWriter writer, IDictionary<string, MemberDescription[]> types, IEnumerable<MemberValue> memberValues)
        {
            foreach (var memberValue in memberValues)
            {
                switch (memberValue.TypeName)
                {
                    case var refType when IsReferenceType(refType):
                        {
                            writer.Write(HashStruct(types, memberValue.TypeName, (IEnumerable<MemberValue>)memberValue.Value));
                            break;
                        }
                    case "string":
                        {
                            var value = Encoding.UTF8.GetBytes((string)memberValue.Value);
                            var abiValueEncoded = Sha3Keccack.Current.CalculateHash(value);
                            writer.Write(abiValueEncoded);
                            break;
                        }
                    case "bytes":
                        {
                            byte[] value;
                            if (memberValue.Value is string)
                            {
                                value = ((string)memberValue.Value).HexToByteArray();
                            }
                            else
                            {
                                value = (byte[])memberValue.Value;
                            }
                            var abiValueEncoded = Sha3Keccack.Current.CalculateHash(value);
                            writer.Write(abiValueEncoded);
                            break;
                        }
                    default:
                        {
                            if (memberValue.TypeName.Contains("["))
                            {
                                var items = (IList)memberValue.Value;
                                var itemsMemberValues = new List<MemberValue>();
                                foreach (var item in items)
                                {
                                    itemsMemberValues.Add(new MemberValue()
                                    {
                                        TypeName = memberValue.TypeName.Substring(0, memberValue.TypeName.LastIndexOf("[")),
                                        Value = item
                                    });
                                }
                                using (var memoryStream = new MemoryStream())
                                using (var writerItem = new BinaryWriter(memoryStream))
                                {
                                    EncodeData(writerItem, types, itemsMemberValues);
                                    writerItem.Flush();
                                    writer.Write(Sha3Keccack.Current.CalculateHash(memoryStream.ToArray()));
                                }

                            }
                            else if (memberValue.TypeName.StartsWith("int") || memberValue.TypeName.StartsWith("uint"))
                            {
                                object value;
                                if (memberValue.Value is string)
                                {
                                    BigInteger parsedOutput;
                                    if (BigInteger.TryParse((string)memberValue.Value, out parsedOutput))
                                    {
                                        value = parsedOutput;
                                    }
                                    else
                                    {
                                        value = memberValue.Value;
                                    }
                                }
                                else
                                {
                                    value = memberValue.Value;
                                }
                                var abiValue = new ABIValue(memberValue.TypeName, value);
                                var abiValueEncoded = _abiEncode.GetABIEncoded(abiValue);
                                writer.Write(abiValueEncoded);
                            }
                            else
                            {
                                var abiValue = new ABIValue(memberValue.TypeName, memberValue.Value);
                                var abiValueEncoded = _abiEncode.GetABIEncoded(abiValue);
                                writer.Write(abiValueEncoded);
                            }
                            break;
                        }
                }
            }


        }

        /// <summary>
        /// For flat messages only, for complex messages with reference type fields use "EncodeTypedData(TypedData typedData).
        /// </summary>
        public TypedData<TDomain> GenerateTypedData<T, TDomain>(T data, TDomain domain, string primaryTypeName)
        {
            var parameters = _parametersEncoder.GetParameterAttributeValues(typeof(T), data).OrderBy(x => x.ParameterAttribute.Order);

            var typeMembers = new List<MemberDescription>();
            var typeValues = new List<MemberValue>();
            foreach (var parameterAttributeValue in parameters)
            {
                typeMembers.Add(new MemberDescription
                {
                    Type = parameterAttributeValue.ParameterAttribute.Type,
                    Name = parameterAttributeValue.ParameterAttribute.Name
                });

                typeValues.Add(new MemberValue
                {
                    TypeName = parameterAttributeValue.ParameterAttribute.Type,
                    Value = parameterAttributeValue.Value
                });
            }

            var result = new TypedData<TDomain>
            {
                PrimaryType = primaryTypeName,
                Types = new Dictionary<string, MemberDescription[]>
                {
                    [primaryTypeName] = typeMembers.ToArray(),
                    ["EIP712Domain"] = MemberDescriptionFactory.GetTypesMemberDescription(typeof(TDomain))["EIP712Domain"]
                },
                Message = typeValues.ToArray(),
                Domain = domain
            };

            return result;
        }
    }
}

namespace Nethereum.Signer.EIP712
{

    /// <summary>
    /// Implementation of EIP-712 signer
    /// https://github.com/ethereum/EIPs/blob/master/EIPS/eip-712.md
    /// </summary>
    public class CustomEip712TypedDataSigner
    {

        private readonly EthereumMessageSigner _signer = new EthereumMessageSigner();
        public static CustomEip712TypedDataSigner Current { get; } = new CustomEip712TypedDataSigner();

        /// <summary>
        /// Encodes data according to EIP-712, hashes it and signs with <paramref name="key"/>.
        /// Infers types of message fields from <see cref="Nethereum.ABI.FunctionEncoding.Attributes.ParameterAttribute"/>.
        /// For flat messages only, for complex messages with reference type fields use "SignTypedData(TypedData typedData, EthECKey key)" method.
        /// </summary>
        public string SignTypedData<T, TDomain>(T data, TDomain domain, string primaryTypeName, EthECKey key)
        {
            var typedData = CustomEip712TypedDataEncoder.Current.GenerateTypedData(data, domain, primaryTypeName);

            return SignTypedData(typedData, key);
        }


        /// <summary>
        /// Encodes data according to EIP-712, hashes it and signs with <paramref name="key"/>.
        /// </summary>
        public string SignTypedData<TDomain>(TypedData<TDomain> typedData, EthECKey key)
        {
            var encodedData = EncodeTypedData(typedData);
            return _signer.HashAndSign(encodedData, key);
        }

        /// <summary>
        /// Encodes data according to EIP-712, hashes it and signs with <paramref name="key"/>.
        /// Matches the signature produced by eth_signTypedData_v4
        /// </summary>
        public string SignTypedDataV4<TDomain>(TypedData<TDomain> typedData, EthECKey key)
        {
            var encodedData = EncodeTypedData(typedData);
            var signature = key.SignAndCalculateV(Sha3Keccack.Current.CalculateHash(encodedData));
            return EthECDSASignature.CreateStringSignature(signature);
        }

        public string SignTypedDataV4(string json, EthECKey key)
        {
            var encodedData = EncodeTypedData(json);
            var signature = key.SignAndCalculateV(Sha3Keccack.Current.CalculateHash(encodedData));
            return EthECDSASignature.CreateStringSignature(signature);
        }

        /// <summary>
        /// Sign typed data using a non standard json (streamlined)
        /// if a Domain type is not included in the json, the generic DomainType will be used
        /// enables using a different message key selector
        /// if a primary type is not set and if it includes only a single type this will be used as the primary type
        /// </summary>
        public string SignTypedDataV4<TDomain>(string json, EthECKey key, string messageKeySelector = "message")
        {
            var encodedData = EncodeTypedData<TDomain>(json, messageKeySelector);
            var signature = key.SignAndCalculateV(Sha3Keccack.Current.CalculateHash(encodedData));
            return EthECDSASignature.CreateStringSignature(signature);
        }


        /// <summary>
        /// Signs using a predefined typed data schema and converts and encodes the provide the message value
        /// </summary>
        public string SignTypedDataV4<T, TDomain>(T message, TypedData<TDomain> typedData, EthECKey key)
        {
            var encodedData = EncodeTypedData(message, typedData);
            var signature = key.SignAndCalculateV(Sha3Keccack.Current.CalculateHash(encodedData));
            return EthECDSASignature.CreateStringSignature(signature);
        }

        public string RecoverFromSignatureV4<T, TDomain>(T message, TypedData<TDomain> typedData, string signature)
        {
            typedData.EnsureDomainRawValuesAreInitialised();
            var encodedData = EncodeTypedData(message, typedData);
            return new MessageSigner().EcRecover(Sha3Keccack.Current.CalculateHash(encodedData), signature);
        }

        public string RecoverFromSignatureV4<TDomain>(TypedData<TDomain> typedData, string signature)
        {
            typedData.EnsureDomainRawValuesAreInitialised();
            var encodedData = EncodeTypedDataRaw(typedData);
            return new MessageSigner().EcRecover(Sha3Keccack.Current.CalculateHash(encodedData), signature);
        }

        public string RecoverFromSignatureV4(string json, string signature, string messageKeySelector = "message")
        {
            var encodedData = EncodeTypedData<Domain>(json, messageKeySelector);
            return new MessageSigner().EcRecover(Sha3Keccack.Current.CalculateHash(encodedData), signature);
        }


        public string RecoverFromSignatureV4(byte[] encodedData, string signature)
        {
            return new MessageSigner().EcRecover(Sha3Keccack.Current.CalculateHash(encodedData), signature);
        }

        public string RecoverFromSignatureHashV4(byte[] hash, string signature)
        {
            return new MessageSigner().EcRecover(hash, signature);
        }

        public byte[] EncodeTypedData<TDomain>(TypedData<TDomain> typedData)
        {
            return CustomEip712TypedDataEncoder.Current.EncodeTypedData(typedData);
        }

        public byte[] EncodeTypedDataRaw(TypedDataRaw typedData)
        {
            return CustomEip712TypedDataEncoder.Current.EncodeTypedDataRaw(typedData);
        }

        public byte[] EncodeTypedData(string json)
        {
            return CustomEip712TypedDataEncoder.Current.EncodeTypedData(json);
        }


        /// <summary>
        /// Encode typed data using a non standard json (streamlined)
        /// if a Domain type is not included in the json, the generic DomainType will be used
        /// enables using a different message key selector
        /// if a primary type is not set and if it includes only a single type this will be used as the primary type
        /// </summary>
        public byte[] EncodeTypedData<TDomain>(string json, string messageKeySelector = "message")
        {
            return CustomEip712TypedDataEncoder.Current.EncodeTypedData<TDomain>(json, messageKeySelector);
        }

        public byte[] EncodeTypedData<T, TDomain>(T message, TypedData<TDomain> typedData)
        {
            return CustomEip712TypedDataEncoder.Current.EncodeTypedData(message, typedData);
        }


    }
}

namespace Nethereum.ABI.EIP712
{
    public static class CustomTypedDataRawJsonConversion
    {

        public static string ToJson(this TypedDataRaw typedDataRaw)
        {
            return SerialiseRawTypedDataToJson(typedDataRaw);
        }

        public static string ToJson<TDomain>(this TypedData<TDomain> typedData)
        {
            typedData.EnsureDomainRawValuesAreInitialised();
            return SerialiseRawTypedDataToJson(typedData);
        }

        public static string ToJson<TMessage, TDomain>(this TypedData<TDomain> typedData, TMessage message)
        {
            return SerialiseTypedDataToJson(typedData, message);
        }

        /// <summary>
        /// Encode typed data using a non standard json (streamlined)
        /// if a Domain type is not included in the json, the generic DomainType will be used
        /// enables using a different message key selector
        /// if a primary type is not set and it includes only a single type this will be used as the primary type
        /// </summary>
        public static TypedDataRaw DeserialiseJsonToRawTypedData<DomainType>(string json, string messageKeySelector = "message")
        {
            var convertor = new ExpandoObjectConverter();
            var jsonDeserialised = JsonConvert.DeserializeObject<IDictionary<string, object>>(json, convertor);
            var types = jsonDeserialised["types"] as IDictionary<string, object>;
            var typeMemberDescriptions = GetMemberDescriptions(types);
            if (!typeMemberDescriptions.ContainsKey("EIP712Domain"))
            {
                var domainMemberDescription = MemberDescriptionFactory.GetTypesMemberDescription(typeof(DomainType)).FirstOrDefault();
                typeMemberDescriptions.Add(domainMemberDescription.Key, domainMemberDescription.Value);
            }

            var domainValues = GetMemberValues((IDictionary<string, object>)jsonDeserialised["domain"], "EIP712Domain", typeMemberDescriptions);
            var primaryType = string.Empty;

            if (jsonDeserialised.ContainsKey("primaryType"))
            {
                primaryType = jsonDeserialised["primaryType"].ToString();
            }
            else
            {
                if (types.Count == 1)
                {
                    primaryType = types.First().Key;
                }
                else
                {
                    throw new Exception("Primary type not set");
                }
            }

            var message = jsonDeserialised[messageKeySelector];
            var messageValues = GetMemberValues((IDictionary<string, object>)message, primaryType, typeMemberDescriptions);

            var rawTypedData = new TypedDataRaw()
            {
                DomainRawValues = domainValues.ToArray(),
                PrimaryType = primaryType,
                Message = messageValues.ToArray(),
                Types = typeMemberDescriptions
            };

            return rawTypedData;
        }


        public static TypedDataRaw DeserialiseJsonToRawTypedData(string json)
        {
            return DeserialiseJsonToRawTypedData<Domain>(json);
        }

        public static string SerialiseTypedDataToJson<TMessage, TDomain>(TypedData<TDomain> typedData, TMessage message)
        {
            typedData.EnsureDomainRawValuesAreInitialised();
            typedData.Message = MemberValueFactory.CreateFromMessage(message);
            return SerialiseRawTypedDataToJson(typedData);
        }

        public static string SerialiseRawTypedDataToJson(TypedDataRaw typedDataRaw)
        {
            var jobject = (JObject)JToken.FromObject(typedDataRaw);
            var domainProperty = new JProperty("domain");
            var domainProperties = GetJProperties("EIP712Domain", typedDataRaw.DomainRawValues, typedDataRaw);
            domainProperty.Value = new JObject(domainProperties.ToArray());
            jobject.Add(domainProperty);
            var messageProperty = new JProperty("message");
            var messageProperties = GetJProperties(typedDataRaw.PrimaryType, typedDataRaw.Message, typedDataRaw);
            messageProperty.Value = new JObject(messageProperties.ToArray());
            jobject.Add(messageProperty);
            return jobject.ToString();
        }
        private static MemberValue GetMemberValue(string memberType, object memberValue, Dictionary<string, MemberDescription[]> typeMemberDescriptions)
        {

            if (CustomEip712TypedDataEncoder.IsReferenceType(memberType))
            {
                return new MemberValue()
                {
                    TypeName = memberType,
                    Value = GetMemberValues((IDictionary<string, object>)memberValue, memberType, typeMemberDescriptions).ToArray()
                };
            }
            else
            {
                if (memberType.StartsWith("bytes"))
                {
                    return new MemberValue()
                    {
                        TypeName = memberType,
                        Value = ((string)memberValue).HexToByteArray()
                    };
                }
                else
                {
                    if (memberType.Contains("["))
                    {
                        var items = (IList)memberValue;
                        var innerType = memberType.Substring(0, memberType.LastIndexOf("["));
                        if (CustomEip712TypedDataEncoder.IsReferenceType(innerType))
                        {
                            var itemsMemberValues = new List<MemberValue[]>();
                            foreach (var item in items)
                            {
                                itemsMemberValues.Add(GetMemberValues((IDictionary<string, object>)item, innerType, typeMemberDescriptions).ToArray());
                            }

                            return new MemberValue() { TypeName = memberType, Value = itemsMemberValues };
                        }
                        else
                        {
                            var itemsMemberValues = new List<object>();

                            foreach (var item in items)
                            {
                                itemsMemberValues.Add(item);
                            }

                            return new MemberValue() { TypeName = memberType, Value = itemsMemberValues };
                        }

                    }
                    else
                    {
                        return new MemberValue()
                        {
                            TypeName = memberType,
                            Value = memberValue
                        };
                    }
                }
            }
        }

        private static Dictionary<string, MemberDescription[]> GetMemberDescriptions(IDictionary<string, object> types)
        {
            var typeMemberDescriptions = new Dictionary<string, MemberDescription[]>();
            foreach (var type in types)
            {
                var memberDescriptions = new List<MemberDescription>();
                foreach (var typeMember in type.Value as List<object>)
                {
                    var typeMemberDictionary = (IDictionary<string, object>)typeMember;
                    memberDescriptions.Add(
                          new MemberDescription()
                          {
                              Name = (string)typeMemberDictionary["name"],
                              Type = (string)typeMemberDictionary["type"]
                          });
                }
                typeMemberDescriptions.Add(type.Key, memberDescriptions.ToArray());
            }

            return typeMemberDescriptions;
        }

        private static List<MemberValue> GetMemberValues(IDictionary<string, object> deserialisedObject, string typeName, Dictionary<string, MemberDescription[]> typeMemberDescriptions)
        {
            var memberValues = new List<MemberValue>();
            var typeMemberDescription = typeMemberDescriptions[typeName];
            foreach (var member in typeMemberDescription)
            {

                var memberType = member.Type;
                var memberValue = deserialisedObject[member.Name];

                memberValues.Add(GetMemberValue(memberType, memberValue, typeMemberDescriptions));
            }

            return memberValues;
        }

        private static List<JProperty> GetJProperties(string mainTypeName, MemberValue[] values, TypedDataRaw typedDataRaw)
        {
            var properties = new List<JProperty>();
            var mainType = typedDataRaw.Types[mainTypeName];
            for (int i = 0; i < mainType.Length; i++)
            {
                var memberType = mainType[i].Type;
                var memberName = mainType[i].Name;
                if (CustomEip712TypedDataEncoder.IsReferenceType(memberType))
                {
                    var memberProperty = new JProperty(memberName);
                    if (values[i].Value != null)
                    {
                        memberProperty.Value = new JObject(GetJProperties(memberType, (MemberValue[])values[i].Value, typedDataRaw).ToArray());
                    }
                    else
                    {
                        memberProperty.Value = null;
                    }
                    properties.Add(memberProperty);
                }
                else
                {
                    if (memberType.StartsWith("bytes"))
                    {
                        var name = memberName;
                        if (values[i].Value is byte[])
                        {
                            var value = ((byte[])values[i].Value).ToHex();
                            properties.Add(new JProperty(name, value));
                        }
                        else
                        {
                            var value = values[i].Value;
                            properties.Add(new JProperty(name, value));
                        }
                    }
                    else
                    {
                        if (memberType.Contains("["))
                        {
                            var memberProperty = new JProperty(memberName);
                            var memberValueArray = new JArray();
                            var innerType = memberType.Substring(0, memberType.LastIndexOf("["));
                            if (values[i].Value == null)
                            {
                                memberProperty.Value = null;
                                properties.Add(memberProperty);
                            }
                            else
                            {
                                if (CustomEip712TypedDataEncoder.IsReferenceType(innerType))
                                {
                                    var items = (List<MemberValue[]>)values[i].Value;

                                    foreach (var item in items)
                                    {
                                        memberValueArray.Add(new JObject(GetJProperties(innerType, item, typedDataRaw).ToArray()));
                                    }
                                    memberProperty.Value = memberValueArray;
                                    properties.Add(memberProperty);
                                }
                                else
                                {
                                    var items = (IList)values[i].Value;

                                    foreach (var item in items)
                                    {
                                        memberValueArray.Add(item);
                                    }

                                    memberProperty.Value = memberValueArray;
                                    properties.Add(memberProperty);
                                }
                            }

                        }
                        else
                        {

                            var name = memberName;
                            var value = values[i].Value;
                            properties.Add(new JProperty(name, value));
                        }
                    }
                }
            }
            return properties;
        }

    }
}