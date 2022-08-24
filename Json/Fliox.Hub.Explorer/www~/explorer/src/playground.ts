import { el, createEl }     from "./types.js";
import { App, app }         from "./index.js";
import { WebSocketClient, WebSocketResponse } from "./websocket.js";
import { SyncRequest } from "../../../../../Json.Tests/assets~/Schema/Typescript/Protocol/Friflo.Json.Fliox.Hub.Protocol.js";


const responseState     = el("response-state");
const subscriptionCount = el("subscriptionCount");
const eventCount        = el("eventCount")      as HTMLSpanElement;
const subscriptionSeq   = el("subscriptionSeq");
const socketStatus      = el("socketStatus");
const reqIdElement      = el("req");
const ackElement        = el("ack");
const cltElement        = el("clt");
const selectExample     = el("example")         as HTMLSelectElement;

//
const defaultUser       = el("user")            as HTMLInputElement;
const defaultToken      = el("token")           as HTMLInputElement;


// ----------------------------------------------- Playground -----------------------------------------------
export class Playground
{
    // --- WebSocket ---
    private wsClient:       WebSocketClient;
    private websocketCount  = 0;
    private eventCount      = 0;                    // number of received events. Reset for every new wsClient

    public getClientId() : string { return this.wsClient?.clt; }

    public connectWebsocket (): void {
        if (this.wsClient) {
            this.wsClient.close();
            this.wsClient = null;
        }
        this.connect();
    }

    public async connect (): Promise<string> {
        if (this.wsClient?.isOpen()) {
            return null;
        }
        try {
            return await this.connectWebSocket();
        } catch (err) {
            const errMsg = `connect failed: ${err}`;
            socketStatus.innerText = errMsg;
            return errMsg;
        }
    }

    // Single requirement to the WebSocket Uri: path have to start with the endpoint. e.g. /fliox/
    private getWebsocketUri() : string {
        const loc       = window.location;
        const protocol  = loc.protocol == "http:" ? "ws:" : "wss:";
        // add websocketCount to path to identify WebSocket in DevTools > Network.
        const nr        = `${++this.websocketCount}`.padStart(3, "0");                  // ws-001
        const endpoint  = loc.pathname.substring(0, loc.pathname.lastIndexOf("/") + 1); // /fliox/    
        const uri       = `${protocol}//${loc.host}${endpoint}ws-${nr}`;
        return uri;
    }

    private async connectWebSocket() : Promise <string> {
        const uri       = this.getWebsocketUri();
        // const uri    = `ws://google.com:8080/`; // test connection timeout
        socketStatus.innerHTML = 'connecting <span class="spinner"></span>';

        this.wsClient = new WebSocketClient();

        this.wsClient.onClose = (e) => {
            socketStatus.innerText = "closed (code: " + e.code + ")";
            responseState.innerText = "";
        };
        this.wsClient.onEvents  = (eventMessages) => {
            const events        = eventMessages.events;
            this.eventCount    += events.length;
            const countStr      = String(this.eventCount);
            subscriptionCount.innerText = countStr;
            eventCount.innerText        = countStr;
            for (const ev of events) {
                app.events.addSubscriptionEvent(ev);
            }
            const lastEv                = events[events.length - 1];
            const subSeq                = lastEv.seq;
            // multiple clients can use the same WebSocket. Use the latest
            subscriptionSeq.innerText   = subSeq ? String(subSeq) : " - ";
            ackElement.innerText        = subSeq ? String(subSeq) : " - ";
        };
        const error     = await this.wsClient.connect(uri);

        this.eventCount = 0;
        if (error) {
            socketStatus.innerText = "error";
            return error;
        }
        socketStatus.innerHTML = "connected <small>🟢</small>";        
        return null;
    }

    public closeWebsocket  () : void {
        this.wsClient.close();
        this.wsClient = null;
    }

    private addUserToken (jsonRequest: string) {
        const endBracket    = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before        = jsonRequest.substring(0, endBracket);
        const after         = jsonRequest.substring(endBracket);
        let   userToken     = JSON.stringify({ user: defaultUser.value, token: defaultToken.value});
        userToken           = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }

    public async sendSyncRequest (): Promise<void> {
        const wsClient = this.wsClient;
        if (!wsClient || !wsClient.isOpen()) {
            app.responseModel.setValue(`Request failed. WebSocket not connected`);
            responseState.innerHTML = "";
            return;
        }
        let jsonRequest     = app.requestModel.getValue();
        jsonRequest         = this.addUserToken(jsonRequest);
        const syncRequest   = JSON.parse(jsonRequest) as SyncRequest;

        // Enable overrides of WebSocket specific members
    //  if (syncRequest.req !== undefined) { this.req           = syncRequest.req; }
    //  if (syncRequest.ack !== undefined) { this.lastEventSeq  = syncRequest.ack; }
    //  if (syncRequest.clt !== undefined) { this.wsClient.clt  = syncRequest.clt; }

        responseState.innerHTML = '<span class="spinner"></span>';
        const response          = await this.sendWsClientRequest(syncRequest);

        const duration          = response.end - response.start;
        const content           = app.formatJson(app.config.formatResponses, response.json);
        app.responseModel.setValue(content);
        responseState.innerHTML = `· ${duration.toFixed(1)} ms`;
    }

    public async sendWebSocketRequest (syncRequest: SyncRequest): Promise<WebSocketResponse> {
        syncRequest.user    = defaultUser.value;
        syncRequest.token   = defaultToken.value;
        return await this.sendWsClientRequest(syncRequest);
    }

    private async sendWsClientRequest (syncRequest: SyncRequest): Promise<WebSocketResponse> {
        // Add WebSocket specific members to request
        const response          = await this.wsClient.syncRequest(syncRequest);

        reqIdElement.innerText  = String(this.wsClient.getReqId());
        cltElement.innerText    = this.wsClient?.clt ?? " - ";
        return response;
    }

    public async postSyncRequest (): Promise<void> {
        let jsonRequest         = app.requestModel.getValue();
        jsonRequest             = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        const start = performance.now();
        let  duration: number;
        try {
            const response  = await App.postRequest(jsonRequest, "POST");
            let content     = response.text;
            content         = app.formatJson(app.config.formatResponses, content);
            duration        = performance.now() - start;
            app.responseModel.setValue(content);
        } catch(error) {
            duration = performance.now() - start;
            app.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration.toFixed(1)} ms`;
    }

    // --------------------------------------- example requests ---------------------------------------
    public async onExampleChange () : Promise<void> {
        const exampleName = selectExample.value;
        if (exampleName == "") {
            app.requestModel.setValue("");
            return;
        }
        const response = await fetch(exampleName);
        const example = await response.text();
        app.requestModel.setValue(example);
    }

    public async loadExampleRequestList () : Promise<void> {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        let option      = createEl("option");
        option.value    = "";
        option.disabled = true;
        option.selected = true;
        option.hidden   = true;
        option.text     = "Select request ...";
        selectExample.add(option);

        const folder    = './explorer/example-requests';
        const response  = await fetch(folder);
        if (!response.ok)
            return;
        const exampleRequests   = await response.json();
        let   groupPrefix       = "0";
        let   groupCount        = 0;
        for (const example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            const name = example.replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = createEl("option");
            option.value                    = folder + "/" + example;
            option.text                     = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor    = groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }    
}
