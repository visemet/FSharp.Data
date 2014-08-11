// --------------------------------------------------------------------------------------
// BSON type provider - methods that are called from the generated erased code
// --------------------------------------------------------------------------------------
namespace FSharp.Data.Runtime

open System
open System.ComponentModel
open System.Globalization
open System.IO
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes

#nowarn "10001"

/// [omit]
type IBsonTop =
    abstract BsonValue : BsonValue

    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
    abstract Path : unit -> string

    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
    abstract Create : value:BsonValue * pathIncrement:string -> IBsonTop

/// Underlying representation of types generated by BsonProvider
[<StructuredFormatDisplay("{_Print}")>]
type BsonTop =
    private {
        /// [omit]
        Value : BsonValue
        /// [omit]
        Path : string
    }

    interface IBsonTop with
        member x.BsonValue = x.Value
        member x.Path() = x.Path
        member x.Create(value, pathIncrement) =
            BsonTop.Create(value, x.Path + pathIncrement)

    /// The underlying BsonValue
    member x.BsonValue = x.Value

    /// [omit]
    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
    member x._Print = x.Value.ToString()

    /// [omit]
    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
    override x.ToString() = x._Print

    /// [omit]
    [<EditorBrowsableAttribute(EditorBrowsableState.Never)>]
    [<CompilerMessageAttribute("This method is intended for use in generated code only.", 10001, IsHidden=true, IsError=false)>]
    static member Create(value, path) =
        { Value = value
          Path = path } :> IBsonTop

/// [omit]
type BsonValueOptionAndPath =
    { BsonOpt : BsonValue option
      Path : string }

/// Static helper methods called from the generated code for working with BSON
type BsonRuntime =

    // --------------------------------------------------------------------------------------
    // bson option -> type

    static member ConvertString = Option.bind BsonConversions.AsString

    static member ConvertBoolean = Option.bind BsonConversions.AsBoolean

    static member ConvertInteger = Option.bind BsonConversions.AsInteger
    
    static member ConvertInteger64 = Option.bind BsonConversions.AsInteger64

    static member ConvertFloat = Option.bind BsonConversions.AsFloat

    static member ConvertDateTime = Option.bind BsonConversions.AsDateTime

    /// Operation that extracts the value from an option and reports a meaningful error message when the value is not there
    /// If the originalValue is a scalar, for missing strings we return "", and for missing doubles we return NaN
    /// For other types an error is thrown
    static member GetNonOptionalValue<'T>(path:string, opt:option<'T>, originalValue) : 'T =
        let getTypeName() = 
            let name = typeof<'T>.Name
            if name.StartsWith "int" 
            then "an " + name
            else "a " + name
        match opt, originalValue with 
        | Some value, _ -> value
        | None, _ when typeof<'T> = typeof<string> -> "" |> unbox
        | None, _ when typeof<'T> = typeof<float> -> Double.NaN |> unbox
        | None, None -> failwithf "'%s' is missing" path
        | None, Some x -> failwithf "Expecting %s at '%s', got %s" (getTypeName()) path <| x.ToString()

    /// Convert BSON array to array of target types
    static member ConvertArray<'T>(top:IBsonTop, mapping:Func<IBsonTop,'T>) =
        let inline bArray (x:'T[]) = BsonArray(x)
        match top.BsonValue.BsonType with
        | BsonType.Array ->
            top.BsonValue.AsBsonArray.Values
            |> Seq.filter (fun value ->
                match value.BsonType with
                | BsonType.Null -> false
                | _ -> true)
            |> Seq.mapi (fun i value -> top.Create(value, sprintf "[%d]" i))
            |> Seq.map mapping.Invoke
            |> Seq.toArray
            |> bArray

        | BsonType.Null -> BsonArray()
        | _ -> failwithf "Expecting a list at '%s', got %A" (top.Path()) top

    /// Optionally get bson property
    static member TryGetPropertyUnpacked(top:IBsonTop, name) =
        match top.BsonValue.BsonType with
        | BsonType.Document ->
            let doc = top.BsonValue.AsBsonDocument
            if doc.Contains name then
                let value = doc.GetValue name
                match value.BsonType with
                | BsonType.Null -> None
                | _ -> Some value
            else None
        | _ -> None

    /// Optionally get bson property and wrap it together with path
    static member TryGetPropertyUnpackedWithPath(top:IBsonTop, name) =
        { BsonOpt = BsonRuntime.TryGetPropertyUnpacked(top, name)
          Path = sprintf "%s/%s" (top.Path()) name }

    /// Optionally get bson property wrapped it in bson document
    static member TryGetPropertyPacked(top:IBsonTop, name) =
        BsonRuntime.TryGetPropertyUnpacked(top, name)
        |> Option.map (fun value -> top.Create(value, sprintf "/%s" name))

    /// Get bson property and wrap it in bson document
    static member GetPropertyPacked(top:IBsonTop, name) =
        match BsonRuntime.TryGetPropertyPacked(top, name) with
        | Some top -> top
        | None -> failwithf "Property '%s' not found at '%s': %A" name (top.Path()) top

    /// Get bson property and wrap it in bson document, return null if not found
    static member GetPropertyPackedOrNull(top:IBsonTop, name) =
        match BsonRuntime.TryGetPropertyPacked(top, name) with
        | Some top -> top
        | None -> top.Create(BsonNull.Value, sprintf "/%s" name)

    /// Optionall get bson property and convert it to the specified type
    static member ConvertOptionalProperty<'T>(top:IBsonTop, name, mapping:Func<IBsonTop,'T>) =
        BsonRuntime.TryGetPropertyPacked(top, name)
        |> Option.map mapping.Invoke

    static member private Matches = function
        | InferedTypeTag.Boolean -> fun (value : BsonValue) ->
            value.IsBoolean
        | InferedTypeTag.Number -> fun (value : BsonValue) ->
            value.IsInt32 || value.IsInt64 || value.IsDouble
        | InferedTypeTag.String -> fun (value : BsonValue) ->
            value.IsString
        | InferedTypeTag.DateTime -> fun (value : BsonValue) ->
            value.IsBsonDateTime
        | InferedTypeTag.Collection -> fun (value : BsonValue) ->
            value.IsBsonArray
        | InferedTypeTag.Record _ -> fun (value : BsonValue) ->
            value.IsBsonDocument
        | tag -> failwithf "%s type not supported" tag.NiceName

    /// Get all array values that match the specified tag
    static member GetArrayChildrenByTypeTag<'T>(top:IBsonTop, tagCode, mapping:Func<IBsonTop,'T>) =
        match top.BsonValue.BsonType with
        | BsonType.Array ->
            top.BsonValue.AsBsonArray.Values
            |> Seq.filter (BsonRuntime.Matches (InferedTypeTag.ParseCode tagCode))
            |> Seq.mapi (fun i value -> top.Create(value, sprintf "[%d]" i))
            |> Seq.map mapping.Invoke
            |> Seq.toArray
        | BsonType.Null -> [| |]
        | x -> failwithf "Expecting an array at '%s', got %s" (top.Path()) <| x.ToString()

    /// Optionally get single array value that matches the specified tag
    static member TryGetArrayChildByTypeTag<'T>(doc, tagCode, mapping:Func<IBsonTop,'T>) =
        match BsonRuntime.GetArrayChildrenByTypeTag(doc, tagCode, mapping) with
        | [| child |] -> Some child
        | [| |] -> None
        | _ -> failwithf "Expecting an array with single or no elements at '%s', got %A" (doc.Path()) doc

    /// Get a single array value that matches the specified tag
    static member GetArrayChildByTypeTag(doc, tagCode) =
        match BsonRuntime.GetArrayChildrenByTypeTag(doc, tagCode, Func<_,_>(id)) with
        | [| child |] -> child
        | _ -> failwithf "Expecting an array with single element at '%s', got %A" (doc.Path()) doc

    static member private ToBsonValue (value:obj) =
        let inline optionToBson f = function
            | None -> BsonNull.Value :> BsonValue
            | Some v -> f v :> BsonValue

        match value with
        | null -> BsonNull.Value :> BsonValue

        | :? IBsonTop as v -> v.BsonValue
        | :? Array as v ->
            BsonArray [| for elem in v -> BsonRuntime.ToBsonValue elem |] :> BsonValue

        // primitive types
        | :? string    as v -> BsonString v :> BsonValue
        | :? DateTime  as v -> BsonDateTime v :> BsonValue
        | :? int       as v -> BsonInt32 v :> BsonValue
        | :? int64     as v -> BsonInt64 v :> BsonValue
        | :? float     as v -> BsonDouble v :> BsonValue
        | :? bool      as v -> BsonBoolean v :> BsonValue
        | :? BsonValue as v -> v

        // option types
        | :? option<string>    as v -> optionToBson (fun x -> BsonString x) v
        | :? option<DateTime>  as v -> optionToBson (fun (x : DateTime) -> BsonDateTime x) v
        | :? option<int>       as v -> optionToBson (fun x -> BsonInt32 x) v
        | :? option<int64>     as v -> optionToBson (fun x -> BsonInt64 x) v
        | :? option<float>     as v -> optionToBson (fun x -> BsonDouble x) v
        | :? option<bool>      as v -> optionToBson (fun x -> BsonBoolean x) v
        | :? option<BsonValue> as v -> optionToBson id v

        | _ -> failwithf "Cannot create BsonValue from %A" value

    /// Creates a BsonValue and wraps it in a bson top
    static member CreateValue value =
        let bson = BsonRuntime.ToBsonValue value
        BsonDocument.Create(bson, "")

    /// Creates a BsonDocument and wraps it in a bson top
    static member CreateDocument properties =
        let inline bDoc (x:seq<BsonElement>) = BsonDocument(x)
        let bson =
            properties
            |> Seq.map (fun (k, v) -> BsonElement(k, BsonRuntime.ToBsonValue v))
            |> bDoc

        BsonDocument.Create(bson, "")

    /// Creates a BsonArray and wraps it in a bson top
    static member CreateArray elements =
        let inline barray (x:BsonValue[]) = BsonArray(x)
        let bson =
            elements
            |> Seq.collect (BsonRuntime.ToBsonValue >> (fun value ->
                match value.BsonType with
                | BsonType.Array -> value.AsBsonArray.Values
                | BsonType.Null -> Seq.empty
                | _ -> Seq.singleton value))
            |> Seq.toArray
            |> barray

        BsonDocument.Create(bson, "")
