/// <reference types="../../../node_modules/monaco-editor/monaco" />
const defaultConfig = {
    showLineNumbers: false,
    showMinimap: false,
    formatEntities: false,
    formatResponses: true,
    activeTab: "explorer",
    showDescription: true,
    filters: {}
};
// --------------------------------------- WebSocket ---------------------------------------
let connection;
let websocketCount = 0;
let req = 1;
let clt = null;
let requestStart;
let subSeq = 0;
let subCount = 0;
function el(id) {
    return document.getElementById(id);
}
const hubInfoEl = el("hubInfo");
const responseState = el("response-state");
const subscriptionCount = el("subscriptionCount");
const subscriptionSeq = el("subscriptionSeq");
const selectExample = el("example");
const socketStatus = el("socketStatus");
const reqIdElement = el("req");
const ackElement = el("ack");
const cltElement = el("clt");
const defaultUser = el("user");
const defaultToken = el("token");
const catalogExplorer = el("catalogExplorer");
const entityExplorer = el("entityExplorer");
const writeResult = el("writeResult");
const readEntitiesDB = el("readEntitiesDB");
const readEntities = el("readEntities");
const catalogSchema = el("catalogSchema");
const entityType = el("entityType");
const entityId = el("entityId");
const entityFilter = el("entityFilter");
const filterRow = el("filterRow");
const commandSignature = el("commandSignature");
const commandLink = el("commandLink");
// request response editor
const requestContainer = el("requestContainer");
const responseContainer = el("responseContainer");
// entity/command editor
const commandValueContainer = el("commandValueContainer");
const commandParamBar = el("commandParamBar");
const commandValue = el("commandValue");
const entityContainer = el("entityContainer");
/* if ("serviceWorker" in navigator) {
    navigator.serviceWorker.register("./sw.js").then(registration => {
        console.log("SW registered");
    }).catch(error => {
        console.error(`SW failed: ${error}`);
    });
} */
class App {
    constructor() {
        this.bracketValue = /\[(.*?)\]/;
        this.selectedEntity = {};
        this.hubInfo = {};
        this.databaseSchemas = {};
        this.schemaLess = '<span title="missing type definition - schema-less database" style="opacity:0.5">unknown</span>';
        this.filter = {};
        this.entityIdentity = {};
        this.entityHistoryPos = -1;
        this.entityHistory = [];
        this.entityModels = {};
        this.commandEditWidth = "60px";
        this.activeExplorerEditor = undefined;
        this.allMonacoSchemas = [];
        this.config = defaultConfig;
    }
    connectWebsocket() {
        if (connection) {
            connection.close();
            connection = null;
        }
        const loc = window.location;
        const nr = ("" + (++websocketCount)).padStart(3, "0");
        const uri = `ws://${loc.host}/ws-${nr}`;
        // const uri  = `ws://google.com:8080/`; // test connection timeout
        socketStatus.innerHTML = 'connecting <span class="spinner"></span>';
        try {
            connection = new WebSocket(uri);
        }
        catch (err) {
            socketStatus.innerText = "connect failed: err";
            return;
        }
        connection.onopen = () => {
            socketStatus.innerHTML = "connected <small>🟢</small>";
            console.log('WebSocket connected');
            req = 1;
            subCount = 0;
        };
        connection.onclose = (e) => {
            socketStatus.innerText = "closed (code: " + e.code + ")";
            responseState.innerText = "";
            console.log('WebSocket closed');
        };
        // Log errors
        connection.onerror = (error) => {
            socketStatus.innerText = "error";
            console.log('WebSocket Error ' + error);
        };
        // Log messages from the server
        connection.onmessage = (e) => {
            const duration = new Date().getTime() - requestStart;
            const data = JSON.parse(e.data);
            // console.log('server:', e.data);
            switch (data.msg) {
                case "resp":
                case "error":
                    clt = data.clt;
                    cltElement.innerText = clt !== null && clt !== void 0 ? clt : " - ";
                    const content = this.formatJson(this.config.formatResponses, e.data);
                    this.responseModel.setValue(content);
                    responseState.innerHTML = `· ${duration} ms`;
                    break;
                case "ev":
                    subscriptionCount.innerText = String(++subCount);
                    subSeq = data.seq;
                    // multiple clients can use the same WebSocket. Use the latest
                    if (clt == data.clt) {
                        subscriptionSeq.innerText = subSeq ? String(subSeq) : " - ";
                        ackElement.innerText = subSeq ? String(subSeq) : " - ";
                    }
                    break;
            }
        };
    }
    closeWebsocket() {
        connection.close();
    }
    getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2)
            return parts.pop().split(';').shift();
        return null;
    }
    initUserToken() {
        var _a, _b;
        const user = (_a = this.getCookie("fliox-user")) !== null && _a !== void 0 ? _a : "admin";
        const token = (_b = this.getCookie("fliox-token")) !== null && _b !== void 0 ? _b : "admin";
        this.setUser(user);
        this.setToken(token);
    }
    setUser(user) {
        defaultUser.value = user;
        document.cookie = `fliox-user=${user};`;
    }
    setToken(token) {
        defaultToken.value = token;
        document.cookie = `fliox-token=${token};`;
    }
    selectUser(element) {
        let value = element.innerText;
        this.setUser(value);
        this.setToken(value);
    }
    ;
    addUserToken(jsonRequest) {
        const endBracket = jsonRequest.lastIndexOf("}");
        if (endBracket == -1)
            return jsonRequest;
        const before = jsonRequest.substring(0, endBracket);
        const after = jsonRequest.substring(endBracket);
        let userToken = JSON.stringify({ user: defaultUser.value, token: defaultToken.value });
        userToken = userToken.substring(1, userToken.length - 1);
        return `${before},${userToken}${after}`;
    }
    sendSyncRequest() {
        if (!connection || connection.readyState != 1) { // 1 == OPEN {
            this.responseModel.setValue(`Request ${req} failed. WebSocket not connected`);
            responseState.innerHTML = "";
        }
        else {
            let jsonRequest = this.requestModel.getValue();
            jsonRequest = this.addUserToken(jsonRequest);
            try {
                const request = JSON.parse(jsonRequest);
                if (request) {
                    // Enable overrides of WebSocket specific members
                    if (request.req !== undefined) {
                        req = request.req;
                    }
                    if (request.ack !== undefined) {
                        subSeq = request.ack;
                    }
                    if (request.clt !== undefined) {
                        clt = request.clt;
                    }
                    // Add WebSocket specific members to request
                    request.req = req;
                    request.ack = subSeq;
                    if (clt) {
                        request.clt = clt;
                    }
                }
                jsonRequest = JSON.stringify(request);
            }
            catch (_a) { }
            responseState.innerHTML = '<span class="spinner"></span>';
            connection.send(jsonRequest);
            requestStart = new Date().getTime();
        }
        req++;
        reqIdElement.innerText = String(req);
    }
    async postSyncRequest() {
        let jsonRequest = this.requestModel.getValue();
        jsonRequest = this.addUserToken(jsonRequest);
        responseState.innerHTML = '<span class="spinner"></span>';
        let start = new Date().getTime();
        let duration;
        try {
            const response = await this.postRequest(jsonRequest, "POST");
            let content = await response.text;
            content = this.formatJson(this.config.formatResponses, content);
            duration = new Date().getTime() - start;
            this.responseModel.setValue(content);
        }
        catch (error) {
            duration = new Date().getTime() - start;
            this.responseModel.setValue("POST error: " + error.message);
        }
        responseState.innerHTML = `· ${duration} ms`;
    }
    applyCtrlKey(event) {
        if (this.lastCtrlKey == event.ctrlKey)
            return;
        this.lastCtrlKey = event.ctrlKey;
        if (!this.refLinkDecoration) {
            const cssRules = document.styleSheets[0].cssRules;
            for (let n = 0; n < cssRules.length; n++) {
                const rule = cssRules[n];
                if (rule.selectorText == ".refLinkDecoration:hover")
                    this.refLinkDecoration = rule;
            }
        }
        this.refLinkDecoration.style.cursor = this.lastCtrlKey ? "pointer" : "";
    }
    onKeyUp(event) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
    }
    onKeyDown(event) {
        if (event.code == "ControlLeft")
            this.applyCtrlKey(event);
        switch (this.config.activeTab) {
            case "playground":
                if (event.code == 'Enter' && event.ctrlKey && event.altKey) {
                    this.sendSyncRequest();
                    event.preventDefault();
                }
                if (event.code == 'KeyP' && event.ctrlKey && event.altKey) {
                    this.postSyncRequest();
                    event.preventDefault();
                }
                if (event.code == 'KeyS' && event.ctrlKey) {
                    // event.preventDefault(); // avoid accidentally opening "Save As" dialog
                }
                break;
            case "explorer":
                switch (event.code) {
                    case 'KeyS':
                        if (event.ctrlKey)
                            this.execute(event, () => this.saveEntity());
                        break;
                    case 'KeyP':
                        if (event.ctrlKey && event.altKey)
                            this.execute(event, () => this.sendCommand("POST"));
                        break;
                    case 'ArrowLeft':
                        if (event.altKey)
                            this.execute(event, () => this.navigateEntity(this.entityHistoryPos - 1));
                        break;
                    case 'ArrowRight':
                        if (event.altKey)
                            this.execute(event, () => this.navigateEntity(this.entityHistoryPos + 1));
                        break;
                }
        }
        // console.log(`KeyboardEvent: code='${event.code}', ctrl:${event.ctrlKey}, alt:${event.altKey}`);
    }
    execute(event, lambda) {
        lambda();
        event.preventDefault();
    }
    // --------------------------------------- example requests ---------------------------------------
    async onExampleChange() {
        const exampleName = selectExample.value;
        if (exampleName == "") {
            this.requestModel.setValue("");
            return;
        }
        const response = await fetch(exampleName);
        const example = await response.text();
        this.requestModel.setValue(example);
    }
    async loadExampleRequestList() {
        // [html - How do I make a placeholder for a 'select' box? - Stack Overflow] https://stackoverflow.com/questions/5805059/how-do-i-make-a-placeholder-for-a-select-box
        let option = document.createElement("option");
        option.value = "";
        option.disabled = true;
        option.selected = true;
        option.hidden = true;
        option.text = "Select request ...";
        selectExample.add(option);
        const folder = './example-requests';
        const response = await fetch(folder);
        if (!response.ok)
            return;
        const exampleRequests = await response.json();
        let groupPrefix = "0";
        let groupCount = 0;
        for (const example of exampleRequests) {
            if (!example.endsWith(".json"))
                continue;
            const name = example.substring(folder.length).replace(".sync.json", "");
            if (groupPrefix != name[0]) {
                groupPrefix = name[0];
                groupCount++;
            }
            option = document.createElement("option");
            option.value = example;
            option.text = (groupCount % 2 ? "\xA0\xA0" : "") + name;
            option.style.backgroundColor = groupCount % 2 ? "#ffffff" : "#eeeeff";
            selectExample.add(option);
        }
    }
    // --------------------------------------- Explorer ---------------------------------------
    async postRequest(request, tag) {
        let init = {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: request
        };
        try {
            const path = `./?${tag}`;
            const rawResponse = await fetch(path, init);
            const text = await rawResponse.text();
            return {
                text: text,
                json: JSON.parse(text)
            };
        }
        catch (error) {
            return {
                text: error.message,
                json: {
                    "msg": "error",
                    "message": error.message
                }
            };
        }
    }
    async postRequestTasks(database, tasks, tag) {
        const db = database == "main_db" ? undefined : database;
        const sync = {
            "msg": "sync",
            "database": db,
            "tasks": tasks,
            "user": defaultUser.value,
            "token": defaultToken.value
        };
        const request = JSON.stringify(sync);
        tag = tag ? tag : "";
        return await this.postRequest(request, `${database}/${tag}`);
    }
    getRestPath(database, container, id, query) {
        let path = `./rest/${database}`;
        if (container)
            path = `${path}/${container}`;
        if (id)
            path = `${path}/${id}`;
        if (query)
            path = `${path}?${query}`;
        return path;
    }
    async restRequest(method, body, database, container, id, query) {
        const path = this.getRestPath(database, container, id, query);
        const init = {
            method: method,
            headers: { 'Content-Type': 'application/json' },
            body: body
        };
        try {
            // authenticate with cookies: "fliox-user" & "fliox-token"
            return await fetch(path, init);
        }
        catch (error) {
            return {
                ok: false,
                text: () => error.message,
                json: () => { throw error.message; }
            };
        }
    }
    getTaskError(content, taskIndex) {
        if (content.msg == "error") {
            return content.message;
        }
        const task = content.tasks[taskIndex];
        if (task.task == "error")
            return "task error:\n" + task.message;
        return undefined;
    }
    errorAsHtml(message, p) {
        // first line: error type, second line: error message
        const pos = message.indexOf(' > ');
        let error = message;
        if (pos > 0) {
            let reason = message.substring(pos + 3);
            if (reason.startsWith("at ")) {
                const id = reason.match(this.bracketValue)[1];
                if (p && id) {
                    const coordinate = JSON.stringify({ database: p.database, container: p.container, id: id });
                    const link = `<a  href="#" onclick='app.loadEntity(${coordinate})'>${id}</a>`;
                    reason = reason.replace(id, link);
                }
                reason = reason.replace("] ", "]<br>");
            }
            error = message.substring(0, pos) + " ><br>" + reason;
        }
        return `<code style="white-space: pre-line; color:red">${error}</code>`;
    }
    setClass(element, enable, className) {
        const classList = element.classList;
        if (enable) {
            classList.add(className);
            return;
        }
        classList.remove(className);
    }
    toggleDescription() {
        this.changeConfig("showDescription", !this.config.showDescription);
        this.openTab(this.config.activeTab);
    }
    openTab(tabName) {
        const config = this.config;
        config.activeTab = tabName;
        this.setClass(document.body, !config.showDescription, "miniHeader");
        const tabContents = document.getElementsByClassName("tabContent");
        const tabs = document.getElementsByClassName("tab");
        const gridTemplateRows = document.body.style.gridTemplateRows.split(" ");
        const headerHeight = getComputedStyle(document.body).getPropertyValue('--header-height');
        gridTemplateRows[0] = config.showDescription ? headerHeight : "0";
        for (let i = 0; i < tabContents.length; i++) {
            const tabContent = tabContents[i];
            const isActiveContent = tabContent.id == tabName;
            tabContent.style.display = isActiveContent ? "grid" : "none";
            gridTemplateRows[i + 2] = isActiveContent ? "1fr" : "0"; // + 2  ->  "body-header" & "body-tabs"
            const isActiveTab = tabs[i].getAttribute('value') == tabName;
            this.setClass(tabs[i], isActiveTab, "selected");
        }
        document.body.style.gridTemplateRows = gridTemplateRows.join(" ");
        this.layoutEditors();
        if (tabName != "settings") {
            this.setConfig("activeTab", tabName);
        }
    }
    setSelectedEntity(elem) {
        if (this.selectedEntity.elem) {
            this.selectedEntity.elem.classList.remove("selected");
        }
        this.selectedEntity.elem = elem;
        this.selectedEntity.elem.classList.add("selected");
    }
    async loadCluster() {
        const tasks = [
            { "task": "query", "container": "containers" },
            { "task": "query", "container": "schemas" },
            { "task": "query", "container": "commands" },
            { "task": "command", "name": "DbHubInfo" }
        ];
        catalogExplorer.innerHTML = 'read databases <span class="spinner"></span>';
        const response = await this.postRequestTasks("cluster", tasks, null);
        const content = response.json;
        const error = this.getTaskError(content, 0);
        if (error) {
            catalogExplorer.innerHTML = this.errorAsHtml(error, null);
            return;
        }
        const dbContainers = content.containers[0].entities;
        const dbSchemas = content.containers[1].entities;
        const dbCommands = content.containers[2].entities;
        const hubInfoResult = content.tasks[3];
        this.hubInfo = hubInfoResult.result;
        //
        let description = this.hubInfo.description;
        const website = this.hubInfo.website;
        if (description || website) {
            if (!description)
                description = "Website";
            hubInfoEl.innerHTML = website ? `<a href="${website}" target="_blank" rel="noopener noreferrer">${description}</a>` : description;
        }
        const ulCatalogs = document.createElement('ul');
        ulCatalogs.onclick = (ev) => {
            const path = ev.composedPath();
            const selectedElement = path[0];
            if (selectedElement.classList.contains("caret")) {
                path[2].classList.toggle("active");
                return;
            }
            path[1].classList.add("active");
            if (this.selectedCatalog)
                this.selectedCatalog.classList.remove("selected");
            this.selectedCatalog = selectedElement;
            selectedElement.classList.add("selected");
            const databaseName = selectedElement.childNodes[1].textContent;
            const commands = dbCommands.find(c => c.id == databaseName);
            const containers = dbContainers.find(c => c.id == databaseName);
            this.listCommands(databaseName, commands, containers);
        };
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const liCatalog = document.createElement('li');
            if (firstDatabase) {
                firstDatabase = false;
                liCatalog.classList.add("active");
            }
            const liDatabase = document.createElement('div');
            const catalogCaret = document.createElement('div');
            catalogCaret.classList.value = "caret";
            const catalogLabel = document.createElement('span');
            catalogLabel.innerText = dbContainer.id;
            liDatabase.title = "database";
            catalogLabel.style.pointerEvents = "none";
            liDatabase.append(catalogCaret);
            liDatabase.append(catalogLabel);
            liCatalog.appendChild(liDatabase);
            ulCatalogs.append(liCatalog);
            const ulContainers = document.createElement('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path = ev.composedPath();
                const selectedElement = path[0];
                // in case of a multiline text selection selectedElement is the parent
                if (selectedElement.tagName.toLowerCase() != "div")
                    return;
                if (this.selectedCatalog)
                    this.selectedCatalog.classList.remove("selected");
                this.selectedCatalog = selectedElement;
                this.selectedCatalog.classList.add("selected");
                const containerName = this.selectedCatalog.innerText.trim();
                const databaseName = path[3].childNodes[0].childNodes[1].textContent;
                const params = { database: databaseName, container: containerName };
                this.clearEntity(databaseName, containerName);
                this.loadEntities(params, null);
            };
            liCatalog.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer = document.createElement('li');
                liContainer.title = "container";
                const containerLabel = document.createElement('div');
                containerLabel.innerHTML = "&nbsp;" + containerName;
                liContainer.append(containerLabel);
                ulContainers.append(liContainer);
            }
        }
        this.createEntitySchemas(dbSchemas);
        catalogExplorer.textContent = "";
        catalogExplorer.appendChild(ulCatalogs);
        this.listCommands(dbCommands[0].id, dbCommands[0], dbContainers[0]);
    }
    createEntitySchemas(dbSchemas) {
        const schemaMap = {};
        for (const dbSchema of dbSchemas) {
            const jsonSchemas = dbSchema.jsonSchemas;
            const database = dbSchema.id;
            const containerRefs = {};
            const rootSchema = jsonSchemas[dbSchema.schemaPath].definitions[dbSchema.schemaName];
            dbSchema._rootSchema = rootSchema;
            const containers = rootSchema.properties;
            for (const containerName in containers) {
                const container = containers[containerName];
                containerRefs[container.additionalProperties.$ref] = containerName;
            }
            this.databaseSchemas[database] = dbSchema;
            dbSchema._containerSchemas = {};
            // add all schemas and their definitions to schemaMap and map them to an uri like:
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json
            //   http://main_db/Friflo.Json.Tests.Common.UnitTest.Fliox.Client.json#/definitions/PocStore
            for (const schemaPath in jsonSchemas) {
                const schema = jsonSchemas[schemaPath];
                const uri = "http://" + database + "/" + schemaPath;
                const schemaEntry = {
                    uri: uri,
                    schema: schema,
                    fileMatch: [],
                    _resolvedDef: schema // not part of monaco > DiagnosticsOptions.schemas
                };
                const namespace = schemaPath.substring(0, schemaPath.length - ".json".length);
                schemaMap[uri] = schemaEntry;
                const definitions = schema.definitions;
                const baseRefType = schema.$ref ? schema.$ref.substring('#/definitions/'.length) : undefined;
                for (const definitionName in definitions) {
                    const definition = definitions[definitionName];
                    definition._typeName = definitionName;
                    definition._namespace = namespace;
                    if (definitionName == baseRefType) {
                        definition._namespace = namespace.substring(0, namespace.length - definitionName.length - 1);
                    }
                    // console.log("---", definition._namespace, definitionName);
                    const path = "/" + schemaPath + "#/definitions/" + definitionName;
                    const schemaId = "." + path;
                    const uri = "http://" + database + path;
                    const containerName = containerRefs[schemaId];
                    if (containerName) {
                        dbSchema._containerSchemas[containerName] = definition;
                    }
                    // add reference for definitionName pointing to definition in current schemaPath
                    const definitionEntry = {
                        uri: uri,
                        schema: {
                            // entityEditor type can either be its entity type or an array using this type
                            "oneOf": [
                                { $ref: schemaId },
                                { type: "array", items: { $ref: schemaId } }
                            ]
                        },
                        fileMatch: [],
                        _resolvedDef: definition // not part of monaco > DiagnosticsOptions.schemas
                    };
                    schemaMap[uri] = definitionEntry;
                }
            }
            this.resolveRefs(jsonSchemas);
            this.addFileMatcher(database, dbSchema, schemaMap);
        }
        const monacoSchemas = Object.values(schemaMap);
        this.addSchemas(monacoSchemas);
    }
    resolveRefs(jsonSchemas) {
        for (const schemaPath in jsonSchemas) {
            // if (schemaPath == "Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Order.json") debugger;
            const schema = jsonSchemas[schemaPath];
            this.resolveNodeRefs(jsonSchemas, schema, schema);
        }
    }
    resolveNodeRefs(jsonSchemas, schema, node) {
        const nodeType = typeof node;
        if (nodeType != "object")
            return;
        if (Array.isArray(node))
            return;
        const ref = node.$ref;
        if (ref) {
            if (ref[0] == "#") {
                const localName = ref.substring("#/definitions/".length);
                node._resolvedDef = schema.definitions[localName];
            }
            else {
                const localNamePos = ref.indexOf("#");
                const schemaPath = ref.substring(2, localNamePos); // start after './'
                const localName = ref.substring(localNamePos + "#/definitions/".length);
                const globalSchema = jsonSchemas[schemaPath];
                node._resolvedDef = globalSchema.definitions[localName];
            }
        }
        for (const propertyName in node) {
            if (propertyName == "_resolvedDef")
                continue;
            // if (propertyName == "employees") debugger;
            const property = node[propertyName];
            this.resolveNodeRefs(jsonSchemas, schema, property);
        }
    }
    // add a "fileMatch" property to all container entity type schemas used for editor validation
    addFileMatcher(database, dbSchema, schemaMap) {
        const jsonSchemas = dbSchema.jsonSchemas;
        const schemaName = dbSchema.schemaName;
        const schemaPath = dbSchema.schemaPath;
        const jsonSchema = jsonSchemas[schemaPath];
        const dbType = jsonSchema.definitions[schemaName];
        const containers = dbType.properties;
        for (const containerName in containers) {
            const container = containers[containerName];
            const containerType = this.getResolvedType(container.additionalProperties, schemaPath);
            const uri = "http://" + database + containerType.$ref.substring(1);
            const schema = schemaMap[uri];
            const url = `entity://${database}.${containerName.toLocaleLowerCase()}.json`;
            schema.fileMatch.push(url); // requires a lower case string
        }
        const commandType = jsonSchema.definitions[schemaName];
        const commands = commandType.commands;
        for (const commandName in commands) {
            const command = commands[commandName];
            // assign file matcher for command param
            const paramType = this.getResolvedType(command.param, schemaPath);
            let url = `command-param://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (paramType.$ref) {
                const uri = "http://" + database + paramType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            }
            else {
                // uri is never referenced - create an arbitrary unique uri
                const uri = "http://" + database + "/command/param" + commandName;
                const schema = {
                    uri: uri,
                    schema: paramType,
                    fileMatch: [url]
                };
                schemaMap[uri] = schema;
            }
            // assign file matcher for command result
            const resultType = this.getResolvedType(command.result, schemaPath);
            url = `command-result://${database}.${commandName.toLocaleLowerCase()}.json`;
            if (resultType.$ref) {
                const uri = "http://" + database + resultType.$ref.substring(1);
                const schema = schemaMap[uri];
                schema.fileMatch.push(url); // requires a lower case string
            }
            else {
                // uri is never referenced - create an arbitrary unique uri
                const uri = "http://" + database + "/command/result" + commandName;
                const schema = {
                    uri: uri,
                    schema: resultType,
                    fileMatch: [url]
                };
                schemaMap[uri] = schema;
            }
        }
    }
    getResolvedType(type, schemaPath) {
        const $ref = type.$ref;
        if (!$ref)
            return type;
        if ($ref[0] != "#")
            return type;
        return { $ref: "./" + schemaPath + $ref };
    }
    getSchemaType(database) {
        const schema = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/html/schema.html" target="${database}">${schema.schemaName}</a>`;
    }
    getSchemaExports(database) {
        const schema = this.databaseSchemas[database];
        if (!schema)
            return this.schemaLess;
        return `<a title="open database schema in new tab" href="./schema/${database}/index.html" target="${database}">Typescript, C#, Kotlin, JSON Schema, HTML</a>`;
    }
    getType(database, def) {
        const ns = def._namespace;
        const name = def._typeName;
        return `<a title="open type definition in new tab" href="./schema/${database}/html/schema.html#${ns}.${name}" target="${database}">${name}</a>`;
    }
    getEntityType(database, container) {
        const dbSchema = this.databaseSchemas[database];
        if (!dbSchema)
            return this.schemaLess;
        const def = dbSchema._containerSchemas[container];
        return this.getType(database, def);
    }
    getTypeLabel(database, type) {
        if (type.type) {
            return type.type;
        }
        const def = type._resolvedDef;
        if (def) {
            return this.getType(database, def);
        }
        let result = JSON.stringify(type);
        return result = result == "{}" ? "any" : result;
    }
    getDatabaseLink(database) {
        return `<a title="open database in new tab" href="./rest/${database}" target="_blank" rel="noopener noreferrer">${database}</a>`;
    }
    setEditorHeader(show) {
        const displayEntity = show == "entity" ? "" : "none";
        const displayCommand = show == "command" ? "" : "none";
        el("entityTools").style.display = displayEntity;
        el("entityHeader").style.display = displayEntity;
        el("commandTools").style.display = displayCommand;
        el("commandHeader").style.display = displayCommand;
    }
    getCommandTags(database, command, signature) {
        let label = this.schemaLess;
        if (signature) {
            const param = this.getTypeLabel(database, signature.param);
            const result = this.getTypeLabel(database, signature.result);
            label = `<span title="command parameter type"><span style="opacity: 0.5;">(param:</span> <span>${param}</span></span><span style="opacity: 0.5;">) : </span><span title="command result type">${result}</span>`;
        }
        const link = `command=${command}`;
        const url = `./rest/${database}?command=${command}`;
        return {
            link: `<a id="commandAnchor" title="command" onclick="app.sendCommand()" href="${url}" target="_blank" rel="noopener noreferrer">${link}</a>`,
            label: label
        };
    }
    async sendCommand(method) {
        const value = this.commandValueEditor.getValue();
        const database = this.entityIdentity.database;
        const command = this.entityIdentity.command;
        if (!method) {
            const commandAnchor = el("commandAnchor");
            let commandValue = value == "null" ? "" : `&value=${value}`;
            const path = this.getRestPath(database, null, null, `command=${command}${commandValue}`);
            commandAnchor.href = path;
            // window.open(path, '_blank');
            return;
        }
        const response = await this.restRequest(method, value, database, null, null, `command=${command}`);
        let content = await response.text();
        content = this.formatJson(this.config.formatResponses, content);
        this.entityEditor.setValue(content);
    }
    setDatabaseInfo(database, dbContainer) {
        el("databaseName").innerHTML = this.getDatabaseLink(database);
        el("databaseSchema").innerHTML = this.getSchemaType(database);
        el("databaseExports").innerHTML = this.getSchemaExports(database);
        el("databaseType").innerHTML = dbContainer.databaseType;
    }
    listCommands(database, dbCommands, dbContainer) {
        this.setDatabaseInfo(database, dbContainer);
        this.setExplorerEditor("dbInfo");
        catalogSchema.innerHTML = this.getSchemaType(database);
        this.setEditorHeader("none");
        filterRow.style.visibility = "hidden";
        entityFilter.style.visibility = "hidden";
        readEntitiesDB.innerHTML = this.getDatabaseLink(database);
        readEntities.innerHTML = "";
        const ulDatabase = document.createElement('ul');
        ulDatabase.classList.value = "database";
        /* const typeLabel = document.createElement('div');
        typeLabel.innerHTML = `<small style="opacity:0.5">type: ${dbContainer.databaseType}</small>`;
        ulDatabase.append(typeLabel); */
        const commandLabel = document.createElement('li');
        const label = '<small style="opacity:0.5; margin-left: 10px;" title="open database commands in new tab">&nbsp;commands</small>';
        commandLabel.innerHTML = `<a href="./rest/${database}?command=DbCommands" target="_blank" rel="noopener noreferrer">${label}</a>`;
        ulDatabase.append(commandLabel);
        const liCommands = document.createElement('li');
        ulDatabase.appendChild(liCommands);
        const ulCommands = document.createElement('ul');
        ulCommands.onclick = (ev) => {
            this.setEditorHeader("command");
            const path = ev.composedPath();
            let selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent
            const tagName = selectedElement.tagName;
            if (tagName == "SPAN" || tagName == "DIV") {
                selectedElement = path[1];
            }
            const commandName = selectedElement.children[0].textContent;
            this.setSelectedEntity(selectedElement);
            this.showCommand(database, commandName);
            if (path[0].classList.contains("command")) {
                this.sendCommand("POST");
            }
        };
        for (const command of dbCommands.commands) {
            const liCommand = document.createElement('li');
            const commandLabel = document.createElement('div');
            commandLabel.innerText = command;
            liCommand.appendChild(commandLabel);
            const runCommand = document.createElement('div');
            runCommand.classList.value = "command";
            runCommand.title = "POST command";
            liCommand.appendChild(runCommand);
            ulCommands.append(liCommand);
        }
        entityExplorer.innerText = "";
        liCommands.append(ulCommands);
        entityExplorer.appendChild(ulDatabase);
    }
    filterOnKeyUp(event) {
        if (event.code != 'Enter')
            return;
        this.applyFilter();
    }
    applyFilter() {
        const database = this.filter.database;
        const container = this.filter.container;
        const filter = entityFilter.value;
        const query = filter.trim() == "" ? null : `filter=${encodeURIComponent(filter)}`;
        const params = { database: database, container: container };
        this.saveFilter(database, container, filter);
        this.loadEntities(params, query);
    }
    removeFilter() {
        const params = { database: this.filter.database, container: this.filter.container };
        this.loadEntities(params, null);
    }
    saveFilter(database, container, filter) {
        const filters = this.config.filters;
        if (filter.trim() == "") {
            const filterDatabase = filters[database];
            if (filterDatabase) {
                delete filterDatabase[container];
            }
        }
        else {
            if (!filters[database])
                filters[database] = {};
            filters[database][container] = [filter];
        }
        this.setConfig("filters", filters);
    }
    updateFilterLink() {
        const filter = entityFilter.value;
        const query = filter.trim() == "" ? "" : `?filter=${encodeURIComponent(filter)}`;
        const url = `./rest/${this.filter.database}/${this.filter.container}${query}`;
        el("filterLink").href = url;
    }
    async loadEntities(p, query) {
        var _a;
        const storedFilter = (_a = this.config.filters[p.database]) === null || _a === void 0 ? void 0 : _a[p.container];
        const filter = storedFilter && storedFilter[0] ? storedFilter[0] : "";
        entityFilter.value = filter;
        const removeFilterVisibility = query ? "" : "hidden";
        el("removeFilter").style.visibility = removeFilterVisibility;
        this.filter.database = p.database;
        this.filter.container = p.container;
        // const tasks =  [{ "task": "query", "container": p.container, "filterJson":{ "op": "true" }}];
        filterRow.style.visibility = "";
        entityFilter.style.visibility = "";
        catalogSchema.innerHTML = this.getSchemaType(p.database) + ' · ' + this.getEntityType(p.database, p.container);
        readEntitiesDB.innerHTML = this.getDatabaseLink(p.database) + "/";
        const containerLink = `<a title="open container in new tab" href="./rest/${p.database}/${p.container}" target="_blank" rel="noopener noreferrer">${p.container}/</a>`;
        readEntities.innerHTML = `${containerLink}<span class="spinner"></span>`;
        const response = await this.restRequest("GET", null, p.database, p.container, null, query);
        const reload = `<span class="reload" title='reload container' onclick='app.loadEntities(${JSON.stringify(p)})'></span>`;
        writeResult.innerHTML = "";
        readEntities.innerHTML = containerLink + reload;
        if (!response.ok) {
            const error = await response.text();
            entityExplorer.innerHTML = this.errorAsHtml(error, p);
            return;
        }
        let content = await response.json();
        const ids = content.map(entity => entity.id);
        const ulIds = document.createElement('ul');
        ulIds.classList.value = "entities";
        ulIds.onclick = (ev) => {
            const path = ev.composedPath();
            const selectedElement = path[0];
            // in case of a multiline text selection selectedElement is the parent
            if (selectedElement.tagName.toLowerCase() != "li")
                return;
            this.setSelectedEntity(selectedElement);
            const entityId = selectedElement.innerText;
            const params = { database: p.database, container: p.container, id: entityId };
            this.loadEntity(params, false, null);
        };
        for (const id of ids) {
            const liId = document.createElement('li');
            liId.innerText = String(id);
            ulIds.append(liId);
        }
        entityExplorer.innerText = "";
        entityExplorer.appendChild(ulIds);
    }
    findContainerEntity(id) {
        const ulIds = entityExplorer.querySelector("ul");
        const children = Array.prototype.slice.call(ulIds.children);
        for (const child of children) {
            if (child.innerText == id) {
                return child;
            }
        }
        return null;
    }
    storeCursor() {
        if (this.entityHistoryPos < 0)
            return;
        this.entityHistory[this.entityHistoryPos].selection = this.entityEditor.getSelection();
    }
    navigateEntity(pos) {
        if (pos < 0 || pos >= this.entityHistory.length)
            return;
        this.storeCursor();
        this.entityHistoryPos = pos;
        const entry = this.entityHistory[pos];
        this.loadEntity(entry.route, true, entry.selection);
    }
    async loadEntity(p, preserveHistory, selection) {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        if (!preserveHistory) {
            this.storeCursor();
            this.entityHistory[++this.entityHistoryPos] = { route: Object.assign({}, p) };
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
        this.entityIdentity = {
            database: p.database,
            container: p.container,
            entityId: p.id
        };
        entityType.innerHTML = this.getEntityType(p.database, p.container);
        const entityLink = this.getEntityLink(p.database, p.container, p.id);
        entityId.innerHTML = `${entityLink}<span class="spinner"></span>`;
        writeResult.innerHTML = "";
        const response = await this.restRequest("GET", null, p.database, p.container, p.id, null);
        let content = await response.text();
        content = this.formatJson(this.config.formatEntities, content);
        entityId.innerHTML = entityLink + this.getEntityReload(p.database, p.container, p.id);
        if (!response.ok) {
            this.setEntityValue(p.database, p.container, content);
            return;
        }
        // console.log(entityJson);
        this.setEntityValue(p.database, p.container, content);
        if (selection)
            this.entityEditor.setSelection(selection);
        // this.entityEditor.focus(); // not useful - annoying: open soft keyboard on phone
    }
    getEntityLink(database, container, id) {
        const containerRoute = { database: database, container: container };
        let link = `<a href="#" style="opacity:0.7; margin-right:20px;" onclick='app.loadEntities(${JSON.stringify(containerRoute)})'>« ${container}</a>`;
        if (id) {
            link += `<a title="open entity in new tab" href="./rest/${database}/${container}/${id}" target="_blank" rel="noopener noreferrer">${id}</a>`;
        }
        return link;
    }
    getEntityReload(database, container, id) {
        const p = { database, container, id };
        return `<span class="reload" title='reload entity' onclick='app.loadEntity(${JSON.stringify(p)}, true)'></span>`;
    }
    clearEntity(database, container) {
        this.setExplorerEditor("entity");
        this.setEditorHeader("entity");
        this.entityIdentity = {
            database: database,
            container: container,
            entityId: undefined
        };
        entityType.innerHTML = this.getEntityType(database, container);
        writeResult.innerHTML = "";
        entityId.innerHTML = this.getEntityLink(database, container, null);
        this.setEntityValue(database, container, "");
    }
    getEntityKeyName(database, container) {
        const schema = this.databaseSchemas[database];
        if (schema) {
            const containerType = schema._containerSchemas[container];
            if (containerType.key) {
                // container has a property "key", if primary key is not "id"
                return containerType.key;
            }
        }
        return "id";
    }
    async saveEntity() {
        const database = this.entityIdentity.database;
        const container = this.entityIdentity.container;
        const jsonValue = this.entityModel.getValue();
        let id = null;
        let isArray;
        try {
            const keyName = this.getEntityKeyName(database, container);
            const value = JSON.parse(jsonValue);
            isArray = Array.isArray(value);
            if (!isArray) {
                id = value[keyName];
            }
        }
        catch (error) {
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        writeResult.innerHTML = 'save <span class="spinner"></span>';
        const response = await this.restRequest("PUT", jsonValue, database, container, id, null);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = `<span style="color:red">Save failed: ${error}</code>`;
            return;
        }
        writeResult.innerHTML = "Save successful";
        // add as HTML element to entityExplorer if new
        if (this.entityIdentity.entityId != id) {
            this.entityIdentity.entityId = id;
            const entityLink = this.getEntityLink(database, container, id);
            entityId.innerHTML = entityLink + this.getEntityReload(database, container, id);
            let liId = this.findContainerEntity(id);
            if (!liId) {
                const ulIds = entityExplorer.querySelector("ul");
                liId = document.createElement('li');
                liId.innerText = id;
                liId.classList = "selected";
                ulIds.append(liId);
            }
            this.setSelectedEntity(liId);
            liId.scrollIntoView();
            this.entityHistory[++this.entityHistoryPos] = { route: { database: database, container: container, id: id } };
            this.entityHistory.length = this.entityHistoryPos + 1;
        }
    }
    async deleteEntity() {
        const id = this.entityIdentity.entityId;
        const container = this.entityIdentity.container;
        const database = this.entityIdentity.database;
        writeResult.innerHTML = 'delete <span class="spinner"></span>';
        const response = await this.restRequest("DELETE", null, database, container, id, null);
        if (!response.ok) {
            const error = await response.text();
            writeResult.innerHTML = `<span style="color:red">Delete failed: ${error}</code>`;
        }
        else {
            writeResult.innerHTML = "Delete successful";
            entityId.innerHTML = "";
            this.setEntityValue(database, container, "");
            const selected = entityExplorer.querySelector(`li.selected`);
            selected.remove();
        }
    }
    getModel(url) {
        this.entityModel = this.entityModels[url];
        if (!this.entityModel) {
            const entityUri = monaco.Uri.parse(url);
            this.entityModel = monaco.editor.createModel(null, "json", entityUri);
            this.entityModels[url] = this.entityModel;
        }
        return this.entityModel;
    }
    setEntityValue(database, container, value) {
        const url = `entity://${database}.${container}.json`;
        const model = this.getModel(url);
        model.setValue(value);
        this.entityEditor.setModel(model);
        if (value == "")
            return;
        const databaseSchema = this.databaseSchemas[database];
        if (!databaseSchema)
            return;
        try {
            const containerSchema = databaseSchema._containerSchemas[container];
            this.decorateJson(this.entityEditor, value, containerSchema, database);
        }
        catch (error) {
            console.error("decorateJson", error);
        }
    }
    decorateJson(editor, value, containerSchema, database) {
        JSON.parse(value); // early out on invalid JSON
        // 1.) [json-to-ast - npm] https://www.npmjs.com/package/json-to-ast
        // 2.) bundle.js created fom npm module 'json-to-ast' via:
        //     [node.js - How to use npm modules in browser? is possible to use them even in local (PC) ? - javascript - Stack Overflow] https://stackoverflow.com/questions/49562978/how-to-use-npm-modules-in-browser-is-possible-to-use-them-even-in-local-pc
        // 3.) browserify main.js | uglifyjs > bundle.js
        //     [javascript - How to get minified output with browserify? - Stack Overflow] https://stackoverflow.com/questions/15590702/how-to-get-minified-output-with-browserify
        const ast = parse(value, { loc: true });
        // console.log ("AST", ast);
        // --- deltaDecorations() -> [ITextModel | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.editor.ITextModel.html
        const newDecorations = [
        // { range: new monaco.Range(7, 13, 7, 22), options: { inlineClassName: 'refLinkDecoration' } }
        ];
        this.addRelationsFromAst(ast, containerSchema, (value, container) => {
            const start = value.loc.start;
            const end = value.loc.end;
            const range = new monaco.Range(start.line, start.column, end.line, end.column);
            const markdownText = `${database}/${container}  \nFollow: (ctrl + click)`;
            const hoverMessage = [{ value: markdownText }];
            newDecorations.push({ range: range, options: { inlineClassName: 'refLinkDecoration', hoverMessage: hoverMessage } });
        });
        editor.deltaDecorations([], newDecorations);
    }
    addRelationsFromAst(ast, schema, addRelation) {
        var _a;
        if (!ast.children) // ast is a 'Literal'
            return;
        for (const child of ast.children) {
            switch (child.type) {
                case "Object":
                    this.addRelationsFromAst(child, schema, addRelation);
                    break;
                case "Array":
                    break;
                case "Property":
                    // if (child.key.value == "employees") debugger;
                    const property = schema.properties[child.key.value];
                    if (!property)
                        continue;
                    const value = child.value;
                    switch (value.type) {
                        case "Literal":
                            const relation = property.relation;
                            if (relation && value.value !== null) {
                                addRelation(value, relation);
                            }
                            break;
                        case "Object":
                            const resolvedDef = property._resolvedDef;
                            if (resolvedDef) {
                                this.addRelationsFromAst(value, resolvedDef, addRelation);
                            }
                            break;
                        case "Array":
                            const resolvedDef2 = (_a = property.items) === null || _a === void 0 ? void 0 : _a._resolvedDef;
                            if (resolvedDef2) {
                                this.addRelationsFromAst(value, resolvedDef2, addRelation);
                            }
                            const relation2 = property.relation;
                            if (relation2) {
                                for (const item of value.children) {
                                    if (item.type == "Literal") {
                                        addRelation(item, relation2);
                                    }
                                }
                            }
                            break;
                    }
                    break;
            }
        }
    }
    setCommandParam(database, command, value) {
        const url = `command-param://${database}.${command}.json`;
        const isNewModel = this.entityModels[url] == undefined;
        const model = this.getModel(url);
        if (isNewModel) {
            model.setValue(value);
        }
        this.commandValueEditor.setModel(model);
    }
    setCommandResult(database, command) {
        const url = `command-result://${database}.${command}.json`;
        const model = this.getModel(url);
        this.entityEditor.setModel(model);
    }
    setExplorerEditor(edit) {
        this.activeExplorerEditor = edit;
        // console.log("editor:", edit);
        const commandActive = edit == "command";
        commandValueContainer.style.display = commandActive ? "" : "none";
        commandParamBar.style.display = commandActive ? "" : "none";
        el("explorerEdit").style.gridTemplateRows = commandActive ? `${this.commandEditWidth} var(--vbar-width) 1fr` : "0 0 1fr";
        const editorActive = edit == "command" || edit == "entity";
        entityContainer.style.display = editorActive ? "" : "none";
        el("dbInfo").style.display = edit == "dbInfo" ? "" : "none";
        //
        this.layoutEditors();
    }
    showCommand(database, commandName) {
        this.setExplorerEditor("command");
        const schema = this.databaseSchemas[database]._rootSchema;
        const signature = schema ? schema.commands[commandName] : null;
        const def = signature ? Object.keys(signature.param).length == 0 ? "null" : "{}" : "null";
        const tags = this.getCommandTags(database, commandName, signature);
        commandSignature.innerHTML = tags.label;
        commandLink.innerHTML = tags.link;
        this.entityIdentity.command = commandName;
        this.entityIdentity.database = database;
        this.setCommandParam(database, commandName, def);
        this.setCommandResult(database, commandName);
    }
    // --------------------------------------- monaco editor ---------------------------------------
    // [Monaco Editor Playground] https://microsoft.github.io/monaco-editor/playground.html#extending-language-services-configure-json-defaults
    async createProtocolSchemas() {
        // configure the JSON language support with schemas and schema associations
        // var schemaUrlsResponse  = await fetch("/protocol/json-schema/directory");
        // var schemaUrls          = await schemaUrlsResponse.json();
        /* var schemas = [{
                uri: "http://myserver/foo-schema.json", // id of the first schema
                // fileMatch: [modelUri.toString()], // associate with our model
                schema: {
                    type: "object",
                    properties: {
                        p1: {
                            enum: ["v1", "v2"]
                        },
                        p2: {
                            $ref: "http://myserver/bar-schema.json" // reference the second schema
                        }
                    }
                }
            }, {
                uri: "http://myserver/bar-schema.json", // id of the second schema
                schema: {
                    type: "object",
                    properties: {
                        q1: {
                            enum: ["x1", "x2"]
                        }
                    }
                }
            }]; */
        const schemas = [];
        try {
            const jsonSchemaResponse = await fetch("schema/protocol/json-schema.json");
            const jsonSchema = await jsonSchemaResponse.json();
            for (const schemaName in jsonSchema) {
                const schema = jsonSchema[schemaName];
                const url = "protocol/json-schema/" + schemaName;
                const schemaEntry = {
                    uri: "http://" + url,
                    schema: schema
                };
                schemas.push(schemaEntry);
            }
        }
        catch (e) {
            console.error("load json-schema.json failed");
        }
        return schemas;
    }
    addSchemas(monacoSchemas) {
        this.allMonacoSchemas.push(...monacoSchemas);
        // [LanguageServiceDefaults | Monaco Editor API] https://microsoft.github.io/monaco-editor/api/interfaces/monaco.languages.json.LanguageServiceDefaults.html
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            schemas: this.allMonacoSchemas
        });
    }
    async setupEditors() {
        // this.setExplorerEditor("none");
        // --- setup JSON Schema for monaco
        const requestUri = monaco.Uri.parse("request://jsonRequest.json"); // a made up unique URI for our model
        const responseUri = monaco.Uri.parse("request://jsonResponse.json"); // a made up unique URI for our model
        const monacoSchemas = await this.createProtocolSchemas();
        for (let i = 0; i < monacoSchemas.length; i++) {
            if (monacoSchemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolRequest.json") {
                monacoSchemas[i].fileMatch = [requestUri.toString()]; // associate with our model
            }
            if (monacoSchemas[i].uri == "http://protocol/json-schema/Friflo.Json.Fliox.Hub.Protocol.ProtocolMessage.json") {
                monacoSchemas[i].fileMatch = [responseUri.toString()]; // associate with our model
            }
        }
        this.addSchemas(monacoSchemas);
        // --- create request editor
        {
            this.requestEditor = monaco.editor.create(requestContainer, { /* model: model */});
            this.requestModel = monaco.editor.createModel(null, "json", requestUri);
            this.requestEditor.setModel(this.requestModel);
            const defaultRequest = `{
    "msg": "sync",
    "tasks": [
        {
        "task":  "command",
        "name":  "DbEcho",
        "value": "Hello World"
        }
    ]
}`;
            this.requestModel.setValue(defaultRequest);
        }
        // --- create response editor
        {
            this.responseEditor = monaco.editor.create(responseContainer, { /* model: model */});
            this.responseModel = monaco.editor.createModel(null, "json", responseUri);
            this.responseEditor.setModel(this.responseModel);
        }
        // --- create entity editor
        {
            this.entityEditor = monaco.editor.create(entityContainer, {});
            this.entityEditor.onMouseDown((e) => {
                if (!e.event.ctrlKey)
                    return;
                if (this.activeExplorerEditor != "entity")
                    return;
                // console.log('mousedown - ', e);
                const value = this.entityEditor.getValue();
                const column = e.target.position.column;
                const line = e.target.position.lineNumber;
                window.setTimeout(() => { this.tryFollowLink(value, column, line); }, 1);
            });
        }
        // --- create command value editor
        {
            this.commandValueEditor = monaco.editor.create(commandValue, {});
            // this.commandValueModel   = monaco.editor.createModel(null, "json");
            // this.commandValueEditor.setModel(this.commandValueModel);
            //this.commandValueEditor.setValue("{}");
        }
        // this.commandResponseModel = monaco.editor.createModel(null, "json");
        this.setEditorOptions();
        window.onresize = () => {
            this.layoutEditors();
        };
    }
    setEditorOptions() {
        const editorSettings = {
            lineNumbers: this.config.showLineNumbers ? "on" : "off",
            minimap: { enabled: this.config.showMinimap ? true : false },
            theme: window.appConfig.monacoTheme,
            mouseWheelZoom: true
        };
        this.requestEditor.updateOptions(Object.assign({}, editorSettings));
        this.responseEditor.updateOptions(Object.assign({}, editorSettings));
        this.entityEditor.updateOptions(Object.assign({}, editorSettings));
        this.commandValueEditor.updateOptions(Object.assign({}, editorSettings));
    }
    tryFollowLink(value, column, line) {
        try {
            JSON.parse(value); // early out invalid JSON
            const ast = parse(value, { loc: true });
            const database = this.entityIdentity.database;
            const databaseSchema = this.databaseSchemas[database];
            const containerSchema = databaseSchema._containerSchemas[this.entityIdentity.container];
            let entity;
            this.addRelationsFromAst(ast, containerSchema, (value, container) => {
                if (entity)
                    return;
                const start = value.loc.start;
                const end = value.loc.end;
                if (start.line <= line && start.column <= column && line <= end.line && column <= end.column) {
                    // console.log(`${resolvedDef.databaseName}/${resolvedDef.containerName}/${value.value}`);
                    entity = { database: database, container: container, id: value.value };
                }
            });
            if (entity) {
                this.loadEntity(entity, false, null);
            }
        }
        catch (error) {
            writeResult.innerHTML = `<span style="color:#FF8C00">Follow link failed: ${error}</code>`;
        }
    }
    setConfig(key, value) {
        this.config[key] = value;
        const elem = el(key);
        if (elem instanceof HTMLInputElement) {
            elem.value = value;
            elem.checked = value;
        }
        const valueStr = JSON.stringify(value, null, 2);
        window.localStorage.setItem(key, valueStr);
    }
    getConfig(key) {
        const valueStr = window.localStorage.getItem(key);
        try {
            return JSON.parse(valueStr);
        }
        catch (e) { }
        return undefined;
    }
    initConfigValue(key) {
        const value = this.getConfig(key);
        if (value == undefined) {
            this.setConfig(key, this.config[key]);
            return;
        }
        this.setConfig(key, value);
    }
    loadConfig() {
        this.initConfigValue("showLineNumbers");
        this.initConfigValue("showMinimap");
        this.initConfigValue("formatEntities");
        this.initConfigValue("formatResponses");
        this.initConfigValue("activeTab");
        this.initConfigValue("showDescription");
        this.initConfigValue("filters");
    }
    changeConfig(key, value) {
        this.setConfig(key, value);
        switch (key) {
            case "showLineNumbers":
            case "showMinimap":
                this.setEditorOptions();
                break;
        }
    }
    formatJson(format, text) {
        if (format) {
            try {
                // const action = editor.getAction("editor.action.formatDocument");
                // action.run();
                const obj = JSON.parse(text);
                return JSON.stringify(obj, null, 4);
            }
            catch (error) { }
        }
        return text;
    }
    layoutEditors() {
        // console.log("layoutEditors - activeTab: " + activeTab)
        switch (this.config.activeTab) {
            case "playground":
                const editors = [
                    { editor: this.responseEditor, elem: responseContainer },
                    { editor: this.requestEditor, elem: requestContainer },
                ];
                this.layoutMonacoEditors(editors);
                break;
            case "explorer":
                // layout from right to left. Otherwise commandValueEditor.clientWidth is 0px;
                const editors2 = [
                    { editor: this.entityEditor, elem: entityContainer },
                    { editor: this.commandValueEditor, elem: commandValue },
                ];
                this.layoutMonacoEditors(editors2);
                break;
        }
    }
    layoutMonacoEditors(pairs) {
        for (let n = pairs.length - 1; n >= 0; n--) {
            const pair = pairs[n];
            if (!pair.editor || !pair.elem.children[0]) {
                pairs.splice(n, 1);
            }
        }
        for (const pair of pairs) {
            const child = pair.elem.children[0];
            child.style.width = "0px"; // required to shrink width.  Found no alternative solution right now.
            child.style.height = "0px"; // required to shrink height. Found no alternative solution right now.
        }
        for (const pair of pairs) {
            pair.editor.layout();
        }
        // set editor width/height to their container width/height
        for (const pair of pairs) {
            const child = pair.elem.children[0];
            child.style.width = pair.elem.clientWidth + "px";
            child.style.height = pair.elem.clientHeight + "px";
        }
    }
    startDrag(event, template, bar, horizontal) {
        // console.log(`drag start: ${event.offsetX}, ${template}, ${bar}`)
        this.dragHorizontal = horizontal;
        this.dragOffset = horizontal ? event.offsetX : event.offsetY;
        this.dragTemplate = el(template);
        this.dragBar = el(bar);
        document.body.style.cursor = "ew-resize";
        event.preventDefault();
    }
    getGridColumns(xy) {
        var _a;
        const prev = this.dragBar.previousElementSibling;
        xy = xy - (this.dragHorizontal ? prev.offsetLeft : prev.offsetTop);
        if (xy < 20)
            xy = 20;
        // console.log (`drag x: ${x}`);
        switch (this.dragTemplate.id) {
            case "playground": return [xy + "px", "var(--bar-width)", "1fr"];
            case "explorer":
                const cols = this.dragTemplate.style.gridTemplateColumns.split(" ");
                switch (this.dragBar.id) { //  [150px var(--bar-width) 200px var(--bar-width) 1fr];
                    case "exBar1": return [xy + "px", cols[1], cols[2], cols[3]];
                    case "exBar2": return [cols[0], cols[1], xy + "px", cols[3]];
                }
                break;
            case "explorerEdit":
                this.commandEditWidth = xy + "px";
                return [this.commandEditWidth, "var(--vbar-width)", "1fr"];
        }
        throw `unhandled condition in getGridColumns() id: ${(_a = this.dragTemplate) === null || _a === void 0 ? void 0 : _a.id}`;
    }
    onDrag(event) {
        if (!this.dragTemplate)
            return;
        // console.log(`  drag: ${event.clientX}`);
        const clientXY = this.dragHorizontal ? event.clientX : event.clientY;
        const xy = clientXY - this.dragOffset;
        const cols = this.getGridColumns(xy);
        if (this.dragHorizontal) {
            this.dragTemplate.style.gridTemplateColumns = cols.join(" ");
        }
        else {
            this.dragTemplate.style.gridTemplateRows = cols.join(" ");
        }
        this.layoutEditors();
        event.preventDefault();
    }
    endDrag() {
        if (!this.dragTemplate)
            return;
        this.dragTemplate = undefined;
        document.body.style.cursor = "auto";
    }
    toggleTheme() {
        let mode = document.documentElement.getAttribute('data-theme');
        mode = mode == 'dark' ? 'light' : 'dark';
        window.setTheme(mode);
        this.setEditorOptions();
    }
    initApp() {
        // --- methods without network requests
        this.loadConfig();
        this.initUserToken();
        this.openTab(app.getConfig("activeTab"));
        // --- methods performing network requests - note: methods are not awaited
        this.loadExampleRequestList();
        this.loadCluster();
    }
}
export const app = new App();
window.addEventListener("keydown", event => app.onKeyDown(event), true);
window.addEventListener("keyup", event => app.onKeyUp(event), true);
//# sourceMappingURL=index.js.map