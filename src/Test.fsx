#r "bin/Debug/MongoDB.Bson.dll"
#r "bin/Debug/FSharp.Data.dll"

open System
open System.IO
open System.Text
open MongoDB.Bson
open FSharp.Data
open FSharp.Data.Runtime

let text = "\x05\x00\x00\x00\x00"
printf "%A" <| Encoding.UTF8.GetBytes(text)
BsonValue.Parse(text)

printf "%x\n" (int <| Encoding.UTF8.GetChars([| 0xc2uy; 0x07uy; 0x00uy; 0x00uy |]).[0])
Encoding.UTF8.GetBytes("\xc2\x07\x00\x00")
Encoding.UTF8.GetBytes("\xfd\ff\x07\x00\x00")
BitConverter.ToInt32([| 0xc2uy; 0x07uy; 0x00uy; 0x00uy |], 0)

let moreText = "\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00"
BsonValue.Parse(moreText)

let evenMoreText = "\x31\x00\x00\x00\x04BSON\x00\x26\x00\x00\x00\x00020\x00\x08\x00\x00\x00awesome\x00\x00011\x00\x33\x33\x33\x33\x33\x33\x14\x40\x00102\x00\xc2\x07\x00\x00\x00\x00"
Encoding.UTF8.GetBytes(evenMoreText)

Encoding.Default

BsonValue.Parse(evenMoreText)

type Simple = BsonProvider<"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00">
let simple = Simple.Parse("\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00")

printf "%A\n" simple.BsonValue
printf "%A\n" simple.Hello

type Complex = BsonProvider<"\x31\x00\x00\x00\x04BSON\x00\x26\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00\x33\x33\x33\x33\x33\x33\x14\x40\x102\x00\xc2\x07\x00\x00\x00\x00">
let complex = Complex.Parse("\x31\x00\x00\x00\x04BSON\x00\x26\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00\x33\x33\x33\x33\x33\x33\x14\x40\x102\x00\xc2\x07\x00\x00\x00\x00")

printf "%A\n" complex.BsonValue
printf "%A\n" complex.Bson.Numbers





