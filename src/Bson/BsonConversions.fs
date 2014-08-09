// --------------------------------------------------------------------------------------
// Helper operations for converting converting bsonValue values to other types
// --------------------------------------------------------------------------------------

namespace FSharp.Data.Runtime

open System
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime

/// Conversions from BsonType to string/int/int64/decimal/float/boolean/datetime/guid options
type BsonConversions =

    static member AsString useNoneForNullOrWhiteSpace (cultureInfo:IFormatProvider) (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.String -> if useNoneForNullOrWhiteSpace then TextConversions.AsString bsonValue.AsString else Some bsonValue.AsString
        | BsonType.Boolean -> Some <| if bsonValue.AsBoolean then "true" else "false"
        | BsonType.Int32 -> Some <| bsonValue.AsInt32.ToString cultureInfo
        | BsonType.Int64 -> Some <| bsonValue.AsInt64.ToString cultureInfo
        | BsonType.Double -> Some <| bsonValue.AsDouble.ToString cultureInfo
        | BsonType.Null when not useNoneForNullOrWhiteSpace -> Some ""
        | _ -> None

    static member AsInteger cultureInfo (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Int32 -> Some bsonValue.AsInt32
        | BsonType.Int64 -> Some <| int bsonValue.AsInt64
        | BsonType.Double -> Some <| int bsonValue.AsDouble
        | BsonType.String -> TextConversions.AsInteger cultureInfo bsonValue.AsString
        | _ -> None

    static member AsInteger64 cultureInfo (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Int32 -> Some <| int64 bsonValue.AsInt32
        | BsonType.Int64 -> Some <| bsonValue.AsInt64
        | BsonType.Double -> Some <| int64 bsonValue.AsDouble
        | BsonType.String -> TextConversions.AsInteger64 cultureInfo bsonValue.AsString
        | _ -> None

    static member AsFloat missingValues useNoneForMissingValues cultureInfo (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Int32 -> Some <| float bsonValue.AsInt32
        | BsonType.Int64 -> Some <| float bsonValue.AsInt64
        | BsonType.Double -> Some bsonValue.AsDouble
        | BsonType.String -> TextConversions.AsFloat missingValues useNoneForMissingValues cultureInfo bsonValue.AsString
        | _ -> None

    static member AsBoolean cultureInfo (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.Boolean -> Some <| bsonValue.AsBoolean
        | BsonType.String -> TextConversions.AsBoolean cultureInfo bsonValue.AsString
        | _ -> None

    static member AsDateTime cultureInfo (bsonValue : BsonValue) =
        match bsonValue.BsonType with
        | BsonType.DateTime -> Some bsonValue.AsBsonDateTime
        | _ -> None
