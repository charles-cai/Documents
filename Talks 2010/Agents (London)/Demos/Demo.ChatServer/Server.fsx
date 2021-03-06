﻿#r "..\\bin\\FSharp.AgentExtensions.dll"

open FSharp.Control
open System.IO
open System.Net
open System.Text
open System.Threading

// ----------------------------------------------------------------------------
// Simple agent

let simpleAgent = Agent<_>.Start(fun agent -> async {
  while true do 
    let! msg = agent.Receive() 
    printfn "Received: %s" msg  })

simpleAgent.Post("Hello world!")  


// ----------------------------------------------------------------------------
// Agent implementing a chat room

type ChatMessage = 
  | GetContent of AsyncReplyChannel<string>
  | SendMessage of string

let agent = Agent<_>.Start(fun agent -> 
  let rec loop messages = async {

    // Pick next message from the mailbox
    let! msg = agent.Receive()
    match msg with 
    | SendMessage msg -> 
        // Add message to the list & continue
        return! loop (msg :: messages)

    | GetContent reply -> 
        // Generate HTML with messages
        let sb = new StringBuilder()
        sb.Append("<ul>\n") |> ignore
        for msg in messages do
          sb.AppendFormat("  <li>{0}</li>\n", msg) |> ignore
        sb.Append("</ul>") |> ignore
        // Send it back as the reply
        reply.Reply(sb.ToString())
        return! loop messages }
  loop [] )


agent.Post(SendMessage "Welcome to F# chat implemented using agents!")
agent.Post(SendMessage "This is my second message to this chat room...")

agent.PostAndReply(GetContent)


// ----------------------------------------------------------------------------
// Encapsulated agent


type ChatRoom() = 
  let agent = Agent<_>.Start(fun agent -> 
    let rec loop messages = async {
      let! msg = agent.Receive()
      match msg with 
      | SendMessage msg -> 
          // Add message to the list & continue
          return! loop (msg :: messages)
      | GetContent reply -> 
          // Generate HTML with messages
          let sb = new StringBuilder()
          sb.Append("<ul>\n") |> ignore
          for msg in messages do
            sb.AppendFormat("  <li>{0}</li>\n", msg) |> ignore
          sb.Append("</ul>") |> ignore
          // Send it back as the reply
          reply.Reply(sb.ToString())
          return! loop messages }
    loop [] )
  member x.SendMessage(msg) = agent.Post(SendMessage msg)
  member x.AsyncGetContent(?timeout) = agent.PostAndAsyncReply(GetContent, ?timeout=timeout) 
  member x.GetContent() = agent.PostAndReply(GetContent)

let room = new ChatRoom()

room.SendMessage("Welcome to F# chat implemented using agents!")
room.SendMessage("This is my second message to this chat room...")
room.GetContent()



// ----------------------------------------------------------------------------
// Exposing the chat agent using a simple HTTP server

let root = @"C:\Tomas\Projects\Demos\FSharp.Agents\Demo.ChatServer\"
let cts = new CancellationTokenSource()

HttpListener.Start
 ("http://localhost:8082/", (fun (request, response) -> async {
  match request.Url.LocalPath with 
  | "/post" -> 
      // Send message to the chat room
      room.SendMessage(request.InputString)
      response.Reply("OK")
  | "/chat" -> 
      // Get messages from the chat room (asynchronously!)
      let! text = room.AsyncGetContent()
      response.Reply(text)
  | s ->
      // Handle an ordinary file request
      let file = 
        root + (if s = "/" then "chat.html" else s.ToLower())
      if File.Exists(file) then 
        let typ = contentTypes.[Path.GetExtension(file)]
        response.Reply(typ, File.ReadAllBytes(file))
      else 
        response.Reply(sprintf "File not found: %s" file) }),
   cts.Token)

cts.Cancel()
