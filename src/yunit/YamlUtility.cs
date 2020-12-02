// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Yunit
{
    internal partial class YamlUtility
    {
        public static JToken ToJToken(string input)
        {
            JToken result = null;

            var parser = new Parser(new StringReader(input));
            parser.Consume<StreamStart>();
            if (!parser.TryConsume<StreamEnd>(out var _))
            {
                parser.Consume<DocumentStart>();
                result = ToJToken(parser);
                parser.Consume<DocumentEnd>();
            }

            return result;
        }

        private static JToken ToJToken(IParser parser)
        {
            switch (parser.Consume<NodeEvent>())
            {
                case Scalar scalar:
                    if (scalar.Style == ScalarStyle.Plain)
                    {
                        return ParseScalar(scalar.Value);
                    }
                    return new JValue(scalar.Value);

                case SequenceStart seq:
                    var array = new JArray();
                    while (!parser.TryConsume<SequenceEnd>(out var _))
                    {
                        array.Add(ToJToken(parser));
                    }
                    return array;

                case MappingStart map:
                    var obj = new JObject();
                    while (!parser.TryConsume<MappingEnd>(out var _))
                    {
                        var key = parser.Consume<Scalar>();
                        var value = ToJToken(parser);

                        obj[key.Value] = value;
                        obj.Property(key.Value);
                    }
                    return obj;

                default:
                    throw new NotSupportedException($"Yaml node '{parser.Current.GetType().Name}' is not supported");
            }
        }

        public static string ToString(JToken token)
        {
            var result = new StringBuilder();
            var emitter = new Emitter(new StringWriter(result));

            emitter.Emit(new StreamStart());
            emitter.Emit(new DocumentStart());
            ToString(token, emitter);
            emitter.Emit(new DocumentEnd(isImplicit: true));
            emitter.Emit(new StreamEnd());

            return result.ToString();
        }

        private static void ToString(JToken token, Emitter emitter)
        {
            switch (token)
            {
                case JValue value:
                    emitter.Emit(new Scalar(value.ToString()));
                    break;

                case JArray arr:
                    emitter.Emit(new SequenceStart(default, default, default, default));
                    foreach (var item in arr)
                    {
                        ToString(item, emitter);
                    }
                    emitter.Emit(new SequenceEnd());
                    break;

                case JObject obj:
                    emitter.Emit(new MappingStart());
                    foreach (var item in obj)
                    {
                        ToString(item.Key, emitter);
                        ToString(item.Value, emitter);
                    }
                    emitter.Emit(new MappingEnd());
                    break;
            }
        }

        private static JToken ParseScalar(string value)
        {
            // https://yaml.org/spec/1.2/2009-07-21/spec.html
            //
            //  Regular expression       Resolved to tag
            //
            //    null | Null | NULL | ~                          tag:yaml.org,2002:null
            //    /* Empty */                                     tag:yaml.org,2002:null
            //    true | True | TRUE | false | False | FALSE      tag:yaml.org,2002:bool
            //    [-+]?[0 - 9]+                                   tag:yaml.org,2002:int(Base 10)
            //    0o[0 - 7] +                                     tag:yaml.org,2002:int(Base 8)
            //    0x[0 - 9a - fA - F] +                           tag:yaml.org,2002:int(Base 16)
            //    [-+] ? ( \. [0-9]+ | [0-9]+ ( \. [0-9]* )? ) ( [eE][-+]?[0 - 9]+ )?   tag:yaml.org,2002:float (Number)
            //    [-+]? ( \.inf | \.Inf | \.INF )                 tag:yaml.org,2002:float (Infinity)
            //    \.nan | \.NaN | \.NAN                           tag:yaml.org,2002:float (Not a number)
            //    *                                               tag:yaml.org,2002:str(Default)
            if (string.IsNullOrEmpty(value) || value == "~" || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return JValue.CreateNull();
            }
            if (bool.TryParse(value, out var b))
            {
                return new JValue(b);
            }
            if (long.TryParse(value, out var l))
            {
                return new JValue(l);
            }
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) &&
                !double.IsNaN(d) && !double.IsPositiveInfinity(d) && !double.IsNegativeInfinity(d))
            {
                return new JValue(d);
            }
            if (value.Equals(".nan", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.NaN);
            }
            if (value.Equals(".inf", StringComparison.OrdinalIgnoreCase) || value.Equals("+.inf", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.PositiveInfinity);
            }
            if (value.Equals("-.inf", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.NegativeInfinity);
            }
            return new JValue(value);
        }
    }
}
