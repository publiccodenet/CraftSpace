////////////////////////////////////////////////////////////////////////
// Accessor.cs
// Copyright (C) 2018 by Don Hopkins, Ground Up Software.


using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;


public class BridgeJsonConverter : JsonConverter {


    private static JsonSerializer _jsonSerializer;


    public static JsonSerializer jsonSerializer
    {
        get
        {
            if (_jsonSerializer == null) {
                _jsonSerializer = new JsonSerializer();
                _jsonSerializer.Converters.Add(new BridgeJsonConverter());
                _jsonSerializer.Converters.Add(new StringEnumConverter());
                
                // Configure serializer settings
                _jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                _jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            }

            return _jsonSerializer;
        }
    }


    public static T ConvertTo<T>(JToken data)
    {
        T result = default(T);

        ConvertToType<T>(data, ref result);

        return result;
    }

    public static bool ConvertToType<T>(JToken data, ref T result)
    {
        //Debug.Log("BridgeExtensions: ConvertToType: T: " + typeof(T) + " data: " + data);

        result = (T)data.ToObject(typeof(T), jsonSerializer);

        //Debug.Log("BridgeExtensions: ConvertToType: result: " + result);

        return true;
    }


    public static bool ConvertToType(JToken data, System.Type objectType, ref object result)
    {
        //Debug.Log("BridgeExtensions: ConvertToType: objectType: " + objectType + " data: " + data);

        result = data.ToObject(objectType, jsonSerializer);

        //Debug.Log("BridgeExtensions: ConvertToType: result: " + result);

        return true;
    }


    public static JToken ConvertFrom(object value, JToken defaultToken = null)
    {
        JToken result = defaultToken;
        ConvertFromType(value, ref result);
        return result;
    }


    public static bool ConvertFromType(object value, ref JToken result)
    {
        //Debug.Log("BridgeExtensions: ConvertFromType: value: " + value + " type: " + ((value == null) ? "NULL" : ("" + value.GetType())));

        if (value == null) {
            result = null;
            return true;
        }

        // Convert bridge objects into object references.
        //BridgeObject bo = value as BridgeObject;
        //if (bo) {
        //    value = "object:" + bo.id;
        //}            

        result = JToken.FromObject(value, jsonSerializer);

        //Debug.Log("BridgeExtensions: ConvertFromType: result: " + result + " TokenType: " + result.Type);

        return true;
    }


    public static bool ConvertToEnum<EnumType>(object obj, ref EnumType result)
    {
        int i = 0;

        if (obj != null) {

            if (obj is JToken) {
                JToken token = (JToken)obj;
                switch (token.Type) {
                    case JTokenType.String:
                        obj = (string)token.ToString();
                        break;
                }
            }

            if (obj is string) {

                var str = (string)obj;

                result =
                    (EnumType)Enum.Parse(
                        typeof(EnumType), 
                        str);

                //Debug.Log("BridgeManager: ConvertToEnum: EnumType: " + typeof(EnumType) + " str: " + str + " result: " + result);

                return true;
            }

            if (obj is JValue) {
                i = ((JValue)obj).ToObject<int>();
            } else {
                return false;
            }

        }

        result = 
            (EnumType)Enum.ToObject(
                typeof(EnumType), 
                i);

        //Debug.Log("BridgeManager: ConvertToEnum: EnumType: " + typeof(EnumType) + " i: " + i + " result: " + result);

        return true;
    }


    public static EnumType ToEnum<EnumType>(object obj)
    {
        EnumType result = default(EnumType);
        ConvertToEnum<EnumType>(obj, ref result);
        return result;
    }


    public static string ConvertFromEnum<EnumType>(EnumType value)
    {
        string result =
            Enum.Format(
                typeof(EnumType), 
                value, 
                "g");

        //Debug.Log("BridgeManager: ConvertFromEnum: EnumType: " + typeof(EnumType) + " value: " + value + " result: " + result);

        return result;
    }


    public static bool ConvertToEnumMask<EnumType>(object obj, ref EnumType result)
    {
        int val = 0;

        if (obj != null) {

            if (obj is JArray) {
                foreach (JToken value in (JArray)obj) {
                    EnumType enumVal = ToEnum<EnumType>(value);
                    int intVal = Convert.ToInt32(enumVal);
                    //Debug.Log("val: " + val + " value: " + value + " intVal: " + intVal + " val now: " + (val | intVal));
                    val |= intVal;
                }
            } else {
                EnumType enumVal = ToEnum<EnumType>(obj);
                val = Convert.ToInt32(enumVal);
            }

        }

        result = (EnumType)Enum.ToObject(typeof(EnumType), val);

        return true;
    }


    public static EnumType ToEnumMask<EnumType>(object obj)
    {
        EnumType result = default(EnumType);
        ConvertToEnumMask<EnumType>(obj, ref result);
        //Debug.Log("Bridge: ToEnumMask: EnumType: " + typeof(EnumType).Name + " obj: " + obj.GetType().Name + " " + obj + " result: " + result);
        return result;
    }


    public static string GetStringDefault(JObject obj, string key, string def = null)
    {
        var valueToken = obj[key];
        if (valueToken == null) {
            return def;
        }

        string str = (string)valueToken;
        if (str == null) {
            return def;
        }

        return str;
    }


    public static JObject GetJObjectDefault(JObject obj, string key, JObject def = null)
    {
        var valueToken = obj[key];
        if (valueToken == null) {
            return def;
        }

        JObject jobj = valueToken as JObject;
        if (jobj == null) {
            return def;
        }

        return jobj;
    }


    public static JArray GetJArrayDefault(JObject obj, string key, JArray def = null)
    {
        var valueToken = obj[key];
        if (valueToken == null) {
            return def;
        }

        JArray jarr = valueToken as JArray;
        if (jarr == null) {
            return def;
        }

        return jarr;
    }


    public void ForceAOTCompilerToIncludeTheseTypes()
    {
        List<Color> colorList = new List<Color>();
        List<Vector2> vector2List = new List<Vector2>();
        List<Vector3> vector3List = new List<Vector3>();
        List<Vector4> vector4List = new List<Vector4>();
        List<Quaternion> quaternionList = new List<Quaternion>();
        List<Matrix4x4> matrixList = new List<Matrix4x4>();
    }


    public delegate bool ConvertToDelegate(
        JsonReader reader, 
        System.Type objectType, 
        ref object result, 
        JsonSerializer serializer);


    public delegate bool ConvertFromDelegate(
        JsonWriter writer, 
        System.Type objectType, 
        object value, 
        JsonSerializer serializer);


    public override bool CanConvert(Type objectType)
    {
        bool canConvertFrom = 
            convertFromObjectMap.ContainsKey(objectType);
        bool canConvertTo = 
            convertToObjectMap.ContainsKey(objectType);
        bool canConvert = 
            canConvertFrom || canConvertTo;

        return canConvert;
    }


    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Type objectType = value.GetType();

        //Debug.Log("BridgeJsonConverter: WriteJson: writer: " + writer + " value: " + value + " serializer: " + serializer);

        if (!convertFromObjectMap.ContainsKey(objectType)) {
            Debug.LogError("BridgeJsonConverter: WriteJson: convertFromObjectMap missing objectType: " + objectType);
            writer.WriteNull();
            return;
        }

        ConvertFromDelegate converter = 
            convertFromObjectMap[objectType];

        //Debug.Log("BridgeJsonConverter: WriteJson: converter: " + converter);

        bool success = 
            converter(
                writer, 
                objectType, 
                value, 
                serializer);

        //Debug.Log("BridgeJsonConverter: WriteJson: success: " + success);

        if (!success) {
            Debug.LogError("BridgeJsonConverter: WriteJson: error converting value: " + value + " to objectType: " + objectType);
        }

    }


    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (!convertToObjectMap.ContainsKey(objectType)) {
            Debug.LogError("BridgeJsonConverter: ReadJson: convertToObjectMap missing objectType: " + objectType);
            return null;
        }

        ConvertToDelegate converter = convertToObjectMap[objectType];
        //Debug.Log("BridgeJsonConverter: ReadJson: converter: " + converter);

        object result = null;

        bool success = 
            converter(
                reader, 
                objectType, 
                ref result, 
                serializer);

        //Debug.Log("BridgeJsonConverter: ReadJson: success: " + success + " result: " + result);

        if (!success) {
            Debug.LogError("BridgeJsonConverter: ReadJson: error converting JSON reader: " + reader + " to objectType: " + objectType);
            return null;
        }

        return result;
    }


    public override bool CanRead
    {
        get { return true; }
    }


    public override bool CanWrite
    {
        get { return true; }
    }


    public static Dictionary<System.Type, ConvertToDelegate> convertToObjectMap =
        new Dictionary<System.Type, ConvertToDelegate>() {

            { typeof(Enum), // value
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {
                    if (reader.TokenType == JsonToken.Null) {
                        result = Enum.ToObject(objectType, 0);
                        return true;
                    } else if (reader.TokenType == JsonToken.String) {
                        string enumString = (string)JValue.Load(reader);

                        result =
                            Enum.Parse(
                                objectType, 
                                enumString);

                        //Debug.Log("BridgeJsonConverter: convertToObjectMap: Enum: enumString: " + enumString + " result: " + result);

                        return true;
                    } else if (reader.TokenType == JsonToken.StartArray) {
                        JArray enumArray = (JArray)JValue.Load(reader);
                        var resultVal = 0;
                        foreach (string enumString in enumArray) {

                            object enumVal =
                                Enum.Parse(
                                    objectType, 
                                    enumString);
                            resultVal |= Convert.ToInt32(enumVal);

                        }

                        result = Enum.ToObject(objectType, resultVal);

                        return true;
                    } else {
                        Debug.LogError("BridgeJsonConverter: convertToObjectMap: Enum: unexpected type: " + reader.TokenType);
                        return false;
                    }

                }
            },

            { typeof(Vector2), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {
                    if (reader.TokenType == JsonToken.Null) {
                        result = Vector2.zero;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    Vector2 vector2 = Vector2.zero;

                    JObject obj = JObject.Load(reader);
                    float x = obj.GetFloat("x");
                    float y = obj.GetFloat("y");

                    result = new Vector2(x, y);
                    return true;
                }
            },

            { typeof(Vector3), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {
                    if (reader.TokenType == JsonToken.Null) {
                        result = Vector3.zero;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    Vector3 vector3 = Vector3.zero;

                    JObject obj = JObject.Load(reader);
                    float x = obj.GetFloat("x");
                    float y = obj.GetFloat("y");
                    float z = obj.GetFloat("z");

                    result = new Vector3(x, y, z);
                    return true;
                }
            },

            { typeof(Vector4), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {
                    if (reader.TokenType == JsonToken.Null) {
                        result = Vector4.zero;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    Vector4 vector4 = Vector4.zero;

                    JObject obj = JObject.Load(reader);
                    float x = obj.GetFloat("x");
                    float y = obj.GetFloat("y");
                    float z = obj.GetFloat("z");
                    float w = obj.GetFloat("w");

                    result = new Vector4(x, y, z, w);
                    return true;
                }
            },

            { typeof(Quaternion), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {
                    if (reader.TokenType == JsonToken.Null) {
                        result = Quaternion.identity;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    Vector4 vector4 = Vector4.zero;
                    JObject obj = JObject.Load(reader);

                    JToken rollToken = obj["roll"];
                    JToken pitchToken = obj["pitch"];
                    JToken yawToken = obj["yaw"];

                    //Debug.Log("BridgeJsonConverter: Quaternion: json: " + obj);

                    if ((rollToken != null) ||
                        (pitchToken != null) ||
                        (yawToken != null)) {

                        float roll = (rollToken == null) ? 0.0f : (float)rollToken;
                        float pitch = (pitchToken == null) ? 0.0f : (float)pitchToken;
                        float yaw = (yawToken == null) ? 0.0f : (float)yawToken;
                        //Debug.Log("BridgeJsonConverter: Quaternion: pitch: " + pitch + " yaw: " + yaw + " roll: " + roll);

                        result = Quaternion.Euler(pitch, yaw, roll);

                    } else {

                        float x = obj.GetFloat("x");
                        float y = obj.GetFloat("y");
                        float z = obj.GetFloat("z");
                        float w = obj.GetFloat("w");
                        //Debug.Log("BridgeJsonConverter: Quaternion: x: " + x + " y: " + y + " z: " + z + " w: " + w);

                        result = new Quaternion(x, y, z, w);

                    }

                    //Quaternion q = (Quaternion)result;
                    //Debug.Log("BridgeJsonConverter: Quaternion result: x: " + q.x + " y: " + q.y + " z: " + q.z + " w: " + q.w);

                    return true;
                }
            },

            { typeof(Color), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    if (reader.TokenType == JsonToken.Null) {
                        result = Color.black;
                        return true;
                    }

                    Color color = Color.black;

                    if (reader.TokenType == JsonToken.String) {
                        string htmlString = (string)JValue.Load(reader);

                        //Debug.Log("BridgeJsonConverter: convertToObjectMap: Color: htmlString: " + htmlString);

                        if (!ColorUtility.TryParseHtmlString(htmlString, out color)) {
                            Debug.LogError("BridgeJsonConverter: convertToObjectMap: Color: invalid htmlString: " + htmlString);
                            return false;
                        }

                        //Debug.Log("BridgeJsonConverter: convertToObjectMap: Color: result: " + color);

                        result = color;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JObject obj = JObject.Load(reader);

                    float r = obj.GetFloat("r");
                    float g = obj.GetFloat("g");
                    float b = obj.GetFloat("b");
                    float a = obj.GetFloat("a", 1.0f);

                    result = new Color(r, g, b, a);
                    return true;
                }
            },

            { typeof(Matrix4x4), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    Matrix4x4 mat = Matrix4x4.zero;

                    if (reader.TokenType == JsonToken.Null) {
                        result = mat;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartArray) {
                        Debug.LogError("BridgeJsonConverter: Matrix4x4: expected array");
                        result = mat;
                        return false;
                    }

                    JArray a = JArray.Load(reader);

                    if (a.Count != 16) {
                        Debug.LogError("BridgeJsonConverter: Matrix4x4: expected array of length 16");
                        result = mat;
                        return false;
                    }

                    // TODO: make sure all elements of the array are numbers.

                    mat.SetColumn(0, new Vector4((float)a[0], (float)a[1], (float)a[2], (float)a[3]));
                    mat.SetColumn(1, new Vector4((float)a[4], (float)a[5], (float)a[6], (float)a[7]));
                    mat.SetColumn(2, new Vector4((float)a[8], (float)a[9], (float)a[10], (float)a[11]));
                    mat.SetColumn(3, new Vector4((float)a[12], (float)a[13], (float)a[14], (float)a[15]));

                    result = mat;
                    return true;
                }
            },

            { typeof(ParticleSystem.MinMaxCurve), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    ParticleSystem.MinMaxCurve minMaxCurve;

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JToken token = JToken.Load(reader);

                    if ((token.Type == JTokenType.Integer) ||
                        (token.Type == JTokenType.Float)) {

                        float constant = (float)token;
                        minMaxCurve = new ParticleSystem.MinMaxCurve(constant);
                        result = minMaxCurve;

                        return true;
                    }

                    JObject obj = (JObject)token;

                    string minMaxCurveType = obj.GetString("minMaxCurveType");

                    switch (minMaxCurveType) {

                        case "Constant": {

                            float constant = obj.GetFloat("constant");

                            result = new ParticleSystem.MinMaxCurve(constant);
                            //Debug.Log("BridgeJsonConverter: MinMaxCurve: minMaxCurveType: Constant: constant:" + constant + " result: " + result);

                            return true;
                        }

                        case "Curve": {

                            float multiplier = obj.GetFloat("multiplier", 1.0f);

                            JToken curveToken = obj["curve"];
                            AnimationCurve curve = null;

                            if (curveToken != null) {
                                curve = (AnimationCurve)curveToken.ToObject(typeof(AnimationCurve), serializer);
                            }

                            minMaxCurve = new ParticleSystem.MinMaxCurve(multiplier, curve);
                            result = minMaxCurve;

                            //Debug.Log("BridgeJsonConverter: MinMaxCurve: minMaxCurveType: Curve: multiplier: " + multiplier + " curve: " + curve + " result: " + result);

                            return true;
                        }

                        case "RandomCurves": {

                            float multiplier = obj.GetFloat("multiplier", 1.0f);

                            JToken minCurveToken = obj["min"];
                            AnimationCurve minCurve = null;
                            if (minCurveToken != null) {
                                minCurve = (AnimationCurve)minCurveToken.ToObject(typeof(AnimationCurve), serializer);
                            }

                            JToken maxCurveToken = obj["max"];
                            AnimationCurve maxCurve = null;
                            if (maxCurveToken != null) {
                                maxCurve = (AnimationCurve)maxCurveToken.ToObject(typeof(AnimationCurve), serializer);
                            }

                            minMaxCurve = new ParticleSystem.MinMaxCurve(multiplier, minCurve, maxCurve);
                            result = minMaxCurve;

                            //Debug.Log("BridgeJsonConverter: convertToObjectMap: MinMaxCurve: minMaxCurveType: RandomCurves: multiplier: " + multiplier + " minCurve: " + minCurve + " maxCurve: " + maxCurve + " result: " + result);

                            return true;
                        }

                        case "RandomConstants": {

                            float minConstant = obj.GetFloat("min");
                            float maxConstant = obj.GetFloat("max", 1.0f);

                            minMaxCurve = new ParticleSystem.MinMaxCurve(minConstant, maxConstant);
                            result = minMaxCurve;

                            //Debug.Log("BridgeJsonConverter: convertToObjectMap: MinMaxCurve: minMaxCurveType: RandomConstants min: " + minConstant + " max: " + maxConstant + " result: " + result);
                            return true;
                        }

                        default: {
                            Debug.LogError("BridgeJsonConverter: convertToObjectMap: MinMaxCurve: unexpected minMaxCurveType: " + minMaxCurveType);
                            return false;
                        }

                    }

                }
            },

            { typeof(ParticleSystem.MinMaxGradient), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    ParticleSystem.MinMaxGradient minMaxGradient;

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JToken token = JToken.Load(reader);
                    JObject obj = (JObject)token;

                    string minMaxGradientType = obj.GetString("minMaxGradientType");

                    if (string.IsNullOrEmpty(minMaxGradientType)) {
                        Color color = (Color)obj.ToObject(typeof(Color), serializer);
                        minMaxGradient = new ParticleSystem.MinMaxGradient(color);
                        minMaxGradient.mode = ParticleSystemGradientMode.Color;
                        result = minMaxGradient;
                        //Debug.Log("BridgeJsonConverter: convertToObjectMap: MinMaxGradient: color:" + color + " result: " + result);
                        return true;
                    }

                    switch (minMaxGradientType) {

                        case "Color": {
                            JToken colorToken = obj["color"];
                            Color color =
                                (colorToken != null)
                                    ? (Color)colorToken.ToObject(typeof(Color), serializer) 
                                    : Color.white;
                            minMaxGradient = new ParticleSystem.MinMaxGradient(color);
                            minMaxGradient.mode = ParticleSystemGradientMode.Color;
                            result = minMaxGradient;
                            return true;
                        }

                        case "Gradient": {
                            JToken gradientToken = obj["gradient"];
                            Gradient gradient =
                                (gradientToken != null)
                                    ? (Gradient)gradientToken.ToObject(typeof(Gradient), serializer) 
                                    : null;
                            minMaxGradient = new ParticleSystem.MinMaxGradient();
                            minMaxGradient.mode = ParticleSystemGradientMode.Gradient;
                            minMaxGradient.gradient = gradient;
                            result = minMaxGradient;
                            return true;
                        }

                        case "TwoColors": {
                            JToken minToken = obj["min"];
                            Color min = 
                                (minToken != null) 
                                    ? (Color)minToken.ToObject(typeof(Color), serializer)
                                    : Color.white;
                            JToken maxToken = obj["max"];
                            Color max =
                                (maxToken != null)
                                    ? (Color)maxToken.ToObject(typeof(Color), serializer)
                                    : Color.white;
                            minMaxGradient = new ParticleSystem.MinMaxGradient();
                            minMaxGradient.mode = ParticleSystemGradientMode.TwoColors;
                            minMaxGradient.colorMin = min;
                            minMaxGradient.colorMax = max;
                            result = minMaxGradient;
                            return true;
                        }

                        case "TwoGradients": {
                            JToken minToken = obj["min"];
                            Gradient gradientMin =
                                (minToken != null) 
                                    ? (Gradient)minToken.ToObject(typeof(Gradient), serializer) 
                                    : null;
                            JToken maxToken = obj["max"];
                            Gradient gradientMax =
                                (maxToken != null) 
                                    ? (Gradient)maxToken.ToObject(typeof(Gradient), serializer) 
                                    : null;
                            minMaxGradient = new ParticleSystem.MinMaxGradient();
                            minMaxGradient.mode = ParticleSystemGradientMode.TwoGradients;
                            minMaxGradient.gradientMin = gradientMin;
                            minMaxGradient.gradientMax = gradientMax;
                            result = minMaxGradient;
                            return true;
                        }

                        case "RandomColor": {
                            JToken gradientToken = obj["gradient"];
                            Gradient gradient =
                                (gradientToken != null) 
                                    ? (Gradient)gradientToken.ToObject(typeof(Gradient), serializer) 
                                    : null;
                            minMaxGradient = new ParticleSystem.MinMaxGradient();
                            minMaxGradient.mode = ParticleSystemGradientMode.RandomColor;
                            minMaxGradient.gradient = gradient;
                            result = minMaxGradient;
                            return true;
                        }

                        default: {
                            Debug.LogError("BridgeJsonConverter: convertToObjectMap: MinMaxGradient: unexpected minMaxGradientType: " + minMaxGradientType);
                            result = null;
                            return false;
                        }

                    }

                }
            },

            { typeof(Gradient), // class
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JObject obj = JObject.Load(reader);

                    List<GradientAlphaKey> gradientAlphaKeysList = new List<GradientAlphaKey>();
                    List<GradientColorKey> gradientColorKeysList = new List<GradientColorKey>();

                    JArray alphaKeys = obj.GetArray("alphaKeys");
                    JArray colorKeys = obj.GetArray("colorKeys");

                    if (alphaKeys != null) {
                        foreach (JToken keyToken in alphaKeys) {
                            //Debug.Log("BridgeJsonConverter: convertToObjectMap: Gradient: alphaKeys: keyToken: " + keyToken);

                            GradientAlphaKey gradientAlphaKey = 
                                (GradientAlphaKey)keyToken.ToObject(typeof(GradientAlphaKey), serializer);

                            gradientAlphaKeysList.Add(gradientAlphaKey);
                        }

                    }

                    if (colorKeys != null) {
                        foreach (JToken keyToken in colorKeys) {
                            //Debug.Log("BridgeJsonConverter: convertToObjectMap: Gradient: colorKeys: keyToken: " + keyToken);

                            GradientColorKey gradientColorKey =
                                (GradientColorKey)keyToken.ToObject(typeof(GradientColorKey), serializer);

                            gradientColorKeysList.Add(gradientColorKey);
                        }

                    }

                    Gradient gradient = new Gradient();
                    gradient.alphaKeys = gradientAlphaKeysList.ToArray();
                    gradient.colorKeys = gradientColorKeysList.ToArray();

                    string mode = obj.GetString("mode");
                    if (!string.IsNullOrEmpty(mode)) {
                        GradientMode gradientMode = gradient.mode;
                        if (!ConvertToEnum<GradientMode>(mode, ref gradientMode)) {
                            Debug.LogError("BridgeJsonConverter: convertToObjectMap: Gradient: invalid gradientMode: " + mode);
                        } else {
                            gradient.mode = gradientMode;
                        }
                    }

                    result = gradient;
                    return true;
                }
            },

            { typeof(AnimationCurve), // class
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: AnimationCurve reader: " + reader);

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JObject obj = JObject.Load(reader);

                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: AnimationCurve obj: " + obj);

                    string animationCurveType = obj.GetString("animationCurveType");
                    AnimationCurve animationCurve = null;

                    switch (animationCurveType) {

                        case "Constant": {
                            animationCurve = AnimationCurve.Constant(
                                obj.GetFloat("timeStart"),
                                obj.GetFloat("timeEnd"),
                                obj.GetFloat("value"));
                            break;
                        }

                        case "EaseInOut": {
                            animationCurve = AnimationCurve.EaseInOut(
                                obj.GetFloat("timeStart"),
                                obj.GetFloat("timeEnd"),
                                obj.GetFloat("valueStart"),
                                obj.GetFloat("valueEnd"));
                            break;
                        }

                        case "Linear": {
                            animationCurve = AnimationCurve.Linear(
                                obj.GetFloat("timeStart"),
                                obj.GetFloat("timeEnd"),
                                obj.GetFloat("valueStart"),
                                obj.GetFloat("valueEnd"));
                            break;
                        }

                        case "Keys": {
                            JArray keys = obj.GetArray("keys");

                            //Debug.Log("BridgeJsonConverter: convertToObjectMap: AnimationCurve obj: " + obj + " keys: " + keys);
                            if (keys == null) {
                                Debug.LogError("BridgeJsonConverter: convertToObjectMap: AnimationCurve: keys should be an array!");
                                return false;
                            }

                            var keyframeList = new List<Keyframe>();
                            foreach (JToken key in keys) {

                                //Debug.Log("BridgeJsonConverter: convertToObjectMap: AnimationCurve key: " + key + " type: " + key.Type);

                                Keyframe keyframe =
                                    (Keyframe)key.ToObject(typeof(Keyframe), serializer);

                                keyframeList.Add(keyframe);
                            }

                            Keyframe[] curveKeys = keyframeList.ToArray();
                            //Debug.Log("BridgeJsonConverter: convertToObjectMap: total keys: " + curveKeys.Length + " curveKeys: " + curveKeys);

                            animationCurve = new AnimationCurve(curveKeys);

                            break;
                        }

                        default: {
                            Debug.LogError("BridgeJsonConverter: convertToObjectMap: AnimationCurve: unexpected animationCurveType: " + animationCurveType);
                            result = null;
                            return false;
                        }
                    }

                    string preWrapMode = obj.GetString("preWrapMode");
                    if (!string.IsNullOrEmpty(preWrapMode)) {
                        WrapMode wrapMode = animationCurve.preWrapMode;
                        if (!ConvertToEnum<WrapMode>(preWrapMode, ref wrapMode)) {
                            Debug.LogError("BridgeJsonConverter: convertToObjectMap: AnimationCurve: invalid preWrapMode: " + preWrapMode);
                        } else {
                            animationCurve.preWrapMode = wrapMode;
                        }
                    }

                    string postWrapMode = obj.GetString("postWrapMode");
                    if (!string.IsNullOrEmpty(postWrapMode)) {
                        WrapMode wrapMode = animationCurve.postWrapMode;
                        if (!ConvertToEnum<WrapMode>(postWrapMode, ref wrapMode)) {
                            Debug.LogError("BridgeJsonConverter: convertToObjectMap: AnimationCurve: invalid postWrapMode: " + postWrapMode);
                        } else {
                            animationCurve.postWrapMode = wrapMode;
                        }
                    }

                    result = animationCurve;
                    return true;
                }
            },

            { typeof(Keyframe), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JObject obj = JObject.Load(reader);

                    float time = obj.GetFloat("time");
                    float value = obj.GetFloat("value");
                    float inTangent = obj.GetFloat("inTangent");
                    float outTangent = obj.GetFloat("outTangent");

                    Keyframe keyframe = 
                        new Keyframe(time, value, inTangent, outTangent);

                    result = keyframe;
                    return true;
                }
            },

            { typeof(GradientColorKey), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JObject obj = JObject.Load(reader);

                    JToken colorToken = obj["color"];

                    Color color =
                        (colorToken == null)
                            ? Color.white
                            : (Color)colorToken.ToObject(typeof(Color), serializer);
                    float time = obj.GetFloat("time");

                    GradientColorKey gradientColorKey = 
                        new GradientColorKey(color, time);

                    result = gradientColorKey;
                    return true;
                }
            },

            { typeof(GradientAlphaKey), // struct
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JObject obj = JObject.Load(reader);

                    float alpha = obj.GetFloat("alpha", 1.0f);
                    float time = obj.GetFloat("time");

                    GradientAlphaKey gradientAlphaKey = 
                        new GradientAlphaKey(alpha, time);

                    result = gradientAlphaKey;
                    return true;
                }
            },

            { typeof(Texture), // class
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: Texture: reader: " + reader + " objectType: " + objectType);

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType == JsonToken.String) {

                        string resourcePath = (string)JValue.Load(reader);
                        //Debug.Log("BridgeJsonConverter: convertToObjectMap: Texture: resourcePath: " + resourcePath);

                        Texture resource = (Texture)Resources.Load(resourcePath);
                        //Debug.Log("BridgeJsonConverter: convertToObjectMap: Texture: resource: " + resource, resource);

                        result = resource;
                        return true;

                    }

                    if (reader.TokenType != JsonToken.StartObject) {
                        return false;
                    }

                    JObject obj = JObject.Load(reader);

                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: Texture: obj: " + obj);

                    if (obj == null) {
                        return false;
                    }

                    string type = obj.GetString("type");

                    if (type == null) {
                        return false;
                    }

                    switch (type) {

                        case "datauri": {
                            string uri = obj.GetString("uri");
                            string dataImagePNGBase64Prefix = "data:image/png;base64,";
                            if (uri.StartsWith(dataImagePNGBase64Prefix)) {
                                string base64 = uri.Substring(dataImagePNGBase64Prefix.Length);
                                byte[] bytes = System.Convert.FromBase64String(base64);
                                Texture2D texture = new Texture2D(1, 1);
                                texture.LoadImage(bytes);
                                result = texture;
                                return true;
                            } else {
                                return false;
                            }
                        }

    #if USE_SOCKETIO && UNITY_EDITOR
                        case "blob": {
                            int blobID = (int)obj["blobID"];
                            Debug.Log("BridgeJsonConverter: convertToObjectMap: Texture: blobID: " + blobID);
                            Texture2D texture = new Texture2D(1, 1);
                            byte[] bytes = BridgeTransportSocketIO.GetBlob(blobID);
                            string fileName = "/tmp/blob_" + blobID + ".png";
                            Debug.Log("BridgeJsonConverter: convertToObjectMap: bytes length: " + bytes.Length + " fileName:\n" + fileName);
                            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
                                fs.Write(bytes, 0, bytes.Length);
                            }
                            texture.LoadImage(bytes);
                            result = texture;
                            return true;
                        }
    #endif

    #if false
                        case "sharedtexture": {
                            int id = obj.GetInteger("id");
                            Texture2D sharedTexture = GetSharedTexture(id);
                            result = sharedTexture;
                            if (result == null) {
                                return false;
                            }
                            return true;
                        }
    #endif

                        case "resource": {
                            string resourcePath = obj.GetString("path");
                            Texture resource = (Texture)Resources.Load(resourcePath);
                            result = resource;
                            return true;
                        }

                        default: {
                            return false;
                        }

                    }

                }
            },

            { typeof(Material), // class
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: Material: reader: " + reader + " objectType: " + objectType);

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.String) {
                        return false;
                    }

                    string resourcePath = (string)JValue.Load(reader);
                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: Material: resourcePath: " + resourcePath);

                    Material resource = (Material)Resources.Load(resourcePath);
                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: Material: resource: " + resource, resource);

                    result = resource;
                    return true;
                }
            },

            { typeof(PhysicsMaterial), // class
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: PhysicMaterial: reader: " + reader + " objectType: " + objectType);

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.String) {
                        return false;
                    }

                    string resourcePath = (string)JValue.Load(reader);
                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: PhysicMaterial: resourcePath: " + resourcePath);

                    PhysicsMaterial resource = (PhysicsMaterial)Resources.Load(resourcePath);
                    //Debug.Log("BridgeJsonConverter: convertToObjectMap: PhysicMaterial: resource: " + resource, resource);

                    result = resource;
                    return true;
                }
            },

            { typeof(Shader), // class
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.String) {
                        return false;
                    }

                    string shaderName = (string)JValue.Load(reader);
                    Shader shader = Shader.Find(shaderName);

                    result = shader;
                    return true;
                }
            },

            { typeof(ComputeBuffer), // class
                delegate(JsonReader reader, System.Type objectType, ref object result, JsonSerializer serializer) {

                    if (reader.TokenType == JsonToken.Null) {
                        result = null;
                        return true;
                    }

                    if (reader.TokenType != JsonToken.String) {
                        return false;
                    }

                    string computeBufferName = (string)JValue.Load(reader);
                    ComputeBuffer computeBuffer = null; // TODO: Look up computeBufferName

                    Debug.LogError("BridgeJsonConverter: convertToObjectMap: ComputeBuffer: computeBufferName: " + computeBufferName + " TODO!!!");

                    result = computeBuffer;
                    return true;
                }
            },

        };


    public static Dictionary<System.Type, ConvertFromDelegate> convertFromObjectMap =
        new Dictionary<System.Type, ConvertFromDelegate>() {

            { typeof(Vector2), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    Vector2 vector2 = (Vector2)value;
                    writer.WriteStartObject();
                    writer.WritePropertyName("x");
                    writer.WriteValue(vector2.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(vector2.y);
                    writer.WriteEndObject();
                    return true;
                }
            },

            { typeof(Vector3), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    Vector3 vector3 = (Vector3)value;
                    writer.WriteStartObject();
                    writer.WritePropertyName("x");
                    writer.WriteValue(vector3.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(vector3.y);
                    writer.WritePropertyName("z");
                    writer.WriteValue(vector3.z);
                    writer.WriteEndObject();
                    return true;
                }
            },

            { typeof(Vector4), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    Vector4 vector4 = (Vector4)value;
                    writer.WriteStartObject();
                    writer.WritePropertyName("x");
                    writer.WriteValue(vector4.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(vector4.y);
                    writer.WritePropertyName("z");
                    writer.WriteValue(vector4.z);
                    writer.WritePropertyName("w");
                    writer.WriteValue(vector4.w);
                    writer.WriteEndObject();
                    return true;
                }
            },

            { typeof(Quaternion), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    Quaternion quaternion = (Quaternion)value;
                    writer.WriteStartObject();
                    writer.WritePropertyName("x");
                    writer.WriteValue(quaternion.x);
                    writer.WritePropertyName("y");
                    writer.WriteValue(quaternion.y);
                    writer.WritePropertyName("z");
                    writer.WriteValue(quaternion.z);
                    writer.WritePropertyName("w");
                    writer.WriteValue(quaternion.w);
                    writer.WriteEndObject();
                    return true;
                }
            },

            { typeof(Color), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    Color color = (Color)value;
                    writer.WriteStartObject();
                    writer.WritePropertyName("r");
                    writer.WriteValue(color.r);
                    writer.WritePropertyName("g");
                    writer.WriteValue(color.g);
                    writer.WritePropertyName("b");
                    writer.WriteValue(color.b);
                    writer.WritePropertyName("a");
                    writer.WriteValue(color.a);
                    writer.WriteEndObject();
                    return true;
                }
            },

            { typeof(Matrix4x4), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    Matrix4x4 mat = (Matrix4x4)value;
                    writer.WriteStartArray();
                    for (int i = 0; i < 16; i++) {
                        writer.WriteValue(mat[i]);
                    }
                    writer.WriteEndArray();
                    return true;
                }
            },

            { typeof(ParticleSystem.MinMaxCurve), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    //ParticleSystem.MinMaxCurve minMaxCurve;
                    writer.WriteStartObject();
                    // TODO
                    writer.WriteEndObject();
                    return true;
                }
            },

            { typeof(ParticleSystem.MinMaxGradient), // struct
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    //ParticleSystem.MinMaxGradient minMaxGradient;
                    writer.WriteStartObject();
                    // TODO
                    writer.WriteEndObject();
                    return true;
                }
            },

            { typeof(Texture), // class
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    return false;
                }
            },

    #if false
            { typeof(AnimationCurve), // class
                delegate(JsonWriter writer, System.Type objectType, object value, JsonSerializer serializer) {
                    AnimationCurve animationCurve = (AnimationCurve)value;
                    result = ConvertFromAnimationCurve(animationCurve);
                    return true;
                }
            },
    #endif

        };


#if false


    ////////////////////////////////////////////////////////////////////////
    // Proxied C# object.


    public static bool ConvertToProxied(JToken data, ref object result)
    {
        if (data.IsNull) {
            result = null;
            return true;
        }

        if (!data.IsString) {
            return false;
        }

        string handle = data.AsString;

        object proxied = ProxyGroup.FindProxied(handle);

        if (proxied == null) {
            Debug.LogError("BridgeJsonConverter: ConvertToProxied: undefined handle: " + handle);
            return false;
        }

        result = proxied;

        return true;
    }


    public static bool ConvertFromProxied(object obj, ref JToken result)
    {
        if (obj == null) {
            result = JToken.Null;
            return true;
        }

        string handle = ProxyGroup.FindHandle(obj);

        if (handle == null) {
            Debug.LogError("BridgeJsonConverter: ConvertFromProxied: can't make handle for obj: " + obj);
            return false;
        }

        result = new JToken(handle);
        return true;
    }


#endif


}
