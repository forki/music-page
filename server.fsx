#r"packages/Suave/lib/net40/Suave.dll"
#r"packages/Suave.Experimental/lib/net40/Suave.Experimental.dll"
#r"packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open Suave
open Suave.Operators
open Suave.Successful
open Suave.Filters
open Suave.Logging
open Suave.RequestErrors
open Suave.Html
open FSharp.Data
open System.Net

let LastKey = "676193b6a1c403295ad2012d7eeab956"
let LastSecret = "a2a920577e5f342118102542eb81a1e4"

type AlbumSearch = JsonProvider<"json/album_search.json">
type ArtistSearch = JsonProvider<"json/artist_search.json">
type AlbumGetInfo = JsonProvider<"json/album_getinfo.json">

type MailSearch = JsonProvider<"json/music_search.json">
type MailPlaylist = JsonProvider<"json/music_playlist.json">

let li = tag "li"
let audio = tag "audio"
let controls = "controls", ""
let preload x = "preload", x
let source = tag "source"
let src x = "src", x
let (<!>) = Async.map

let musicSearchUrl query = 
  "https://my.mail.ru/cgi-bin/my/ajax?xemail=&ajax_call=1&func_name=music.search&mna=&mnb=&arg_query="
  + WebUtility.HtmlEncode query
  + "&arg_extended=1&arg_search_params=%7B%22music%22%3A%7B%22limit%22%3A100%7D%2C%22playlist%22%3A%7B%22limit%22%3A50%7D%2C%22album%22%3A%7B%22limit%22%3A10%7D%2C%22artist%22%3A%7B%22limit%22%3A10%7D%7D&arg_offset=0&arg_limit=100&_=1496852533447"

let mailHeaders = 
  ["X-Requested-With", "XMLHttpRequest"]

let musicSearch query =
  MailSearch.Parse <!> Http.AsyncRequestString (musicSearchUrl query, headers = mailHeaders)

let  musicPlaylistUrl (id: int64) = 
  "https://my.mail.ru/cgi-bin/my/ajax?xemail=&ajax_call=1&func_name=music.playlist&mna=&mnb=&arg_playlist_id="
  + string id 
  + "&arg_ref_user=&arg_limit=100&arg_ret_json=1&arg_offset=0&_=1496852533450"

let musicPlaylist (id: int64) = 
  MailPlaylist.Parse <!> Http.AsyncRequestString (musicPlaylistUrl id, headers = mailHeaders)

let renderTrack (t: MailPlaylist.Datum) = 
  div [] [
    p [] (text t.Name)
    audio [ controls; preload "none" ] [
      source [ src t.Url ] []
    ]
  ]

let renderAlbum (a: MailPlaylist.Record) =
  let info = a.Collection
  html [] [
    head [] [
      sprintf "%s %s" info.AuthorUrlTextHtml info.Name |> title []
    ]
    body [] (img [ src info.CoverUrl ] :: (Seq.map renderTrack a.Data |> Seq.toList))
  ]

let search (artist, album) x =
    async {
      let! s = sprintf "%s %s" artist album |> musicSearch
      let! a = s.Record.AlbumData.[0].Id |> musicPlaylist

      return! Successful.OK (renderAlbum a.Record |> renderHtmlDocument) x
    }

let app = 
    choose [ GET >=> pathScan "/%s/%s" search;
             NOT_FOUND "404"
           ]

let config =
    let ip = IPAddress.Parse "0.0.0.0"
    { defaultConfig with
        bindings=[ HttpBinding.create HTTP ip 8080us ] }

startWebServer config app
