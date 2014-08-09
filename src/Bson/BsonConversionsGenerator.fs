// ----------------------------------------------------------------------------------------------
// Conversions from string to various primitive types
// ----------------------------------------------------------------------------------------------

module ProviderImplementation.BsonConversionsGenerator

open System
open Microsoft.FSharp.Quotations
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime
open FSharp.Data.Runtime.StructuralTypes
open ProviderImplementation
open ProviderImplementation.QuotationBuilder

#nowarn "10001"

let getConversionQuotation missingValuesStr cultureStr typ (value:Expr<BsonValue option>) =
  if typ = typeof<string> then <@@ BsonRuntime.ConvertString(cultureStr, %value) @@>
  elif typ = typeof<int> || typ = typeof<Bit0> || typ = typeof<Bit1> then <@@ BsonRuntime.ConvertInteger(cultureStr, %value) @@>
  elif typ = typeof<int64> then <@@ BsonRuntime.ConvertInteger64(cultureStr, %value) @@>
  elif typ = typeof<float> then <@@ BsonRuntime.ConvertFloat(cultureStr, missingValuesStr, %value) @@>
  elif typ = typeof<bool> || typ = typeof<Bit> then <@@ BsonRuntime.ConvertBoolean(cultureStr, %value) @@>
  elif typ = typeof<DateTime> then <@@ BsonRuntime.ConvertDateTime(cultureStr, %value) @@>
  else failwith "getConversionQuotation: Unsupported primitive type"

type BsonConversionCallingType = 
    BsonDocument | BsonValueOption | BsonValueOptionAndPath

/// Creates a function that takes Expr<BsonValue option> and converts it to 
/// an expression of other type - the type is specified by `field`
let convertBsonValue (replacer:AssemblyReplacer) missingValuesStr cultureStr canPassAllConversionCallingTypes (field:PrimitiveInferedProperty) = 

  assert (field.TypeWithMeasure = field.RuntimeType)
  assert (field.Name = "")

  let returnType = 
    match field.TypeWrapper with
    | TypeWrapper.None -> field.RuntimeType
    | TypeWrapper.Option -> typedefof<option<_>>.MakeGenericType field.RuntimeType
    | TypeWrapper.Nullable -> typedefof<Nullable<_>>.MakeGenericType field.RuntimeType
    |> replacer.ToRuntime

  let wrapInLetIfNeeded (value:Expr) getBody =
    match value with
    | Patterns.Var var ->
        let varExpr = Expr.Cast<'T> (Expr.Var var)
        getBody varExpr
    | _ ->
        let var = Var("value", typeof<'T>)
        let varExpr = Expr.Cast<'T> (Expr.Var var)
        Expr.Let(var, value, getBody varExpr)

  let convert (value:Expr) =
    let convert value = 
      getConversionQuotation missingValuesStr cultureStr field.InferedType value
    match field.TypeWrapper, canPassAllConversionCallingTypes with
    | TypeWrapper.None, true ->
        wrapInLetIfNeeded value <| fun (varExpr:Expr<BsonValueOptionAndPath>) ->
          typeof<BsonRuntime>?GetNonOptionalValue (field.RuntimeType) (<@ (%varExpr).Path @>, convert <@ (%varExpr).BsonOpt @>, <@ (%varExpr).BsonOpt @>)
    | TypeWrapper.None, false ->
        wrapInLetIfNeeded value <| fun (varExpr:Expr<IBsonDocument>) ->
          typeof<BsonRuntime>?GetNonOptionalValue (field.RuntimeType) (<@ (%varExpr).Path() @>, convert <@ Some (%varExpr).BsonValue @>, <@ Some (%varExpr).BsonValue @>)
    | TypeWrapper.Option, true ->
        convert <@ (%%value:BsonValue option) @>
    | TypeWrapper.Option, false ->
        //TODO: not covered in tests
        convert <@ Some (%%value:IBsonDocument).BsonValue @>
    | TypeWrapper.Nullable, true -> 
        //TODO: not covered in tests
        typeof<TextRuntime>?OptionToNullable (field.RuntimeType) (convert <@ (%%value:BsonValue option) @>)
    | TypeWrapper.Nullable, false -> 
        //TODO: not covered in tests
        typeof<TextRuntime>?OptionToNullable (field.RuntimeType) (convert <@ Some (%%value:IBsonDocument).BsonValue @>)
    |> replacer.ToRuntime

  let conversionCallingType =
    if canPassAllConversionCallingTypes then
        match field.TypeWrapper with
        | TypeWrapper.None -> BsonValueOptionAndPath
        | TypeWrapper.Option | TypeWrapper.Nullable -> BsonValueOption
    else BsonDocument

  returnType, convert, conversionCallingType
