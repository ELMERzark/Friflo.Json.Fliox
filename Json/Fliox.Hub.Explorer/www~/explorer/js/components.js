import { createEl } from "./types.js";
export class ClusterTree {
    selectTreeElement(element) {
        if (this.selectedTreeEl)
            this.selectedTreeEl.classList.remove("selected");
        this.selectedTreeEl = element;
        element.classList.add("selected");
    }
    createClusterUl(dbContainers) {
        const ulCluster = createEl('ul');
        ulCluster.onclick = (ev) => {
            const path = ev.composedPath();
            const databaseEl = ClusterTree.findTreeEl(path, "clusterDatabase");
            const caretEl = ClusterTree.findTreeEl(path, "caret");
            if (caretEl) {
                databaseEl.parentElement.classList.toggle("active");
                return;
            }
            const treeEl = databaseEl.parentElement;
            if (this.selectedTreeEl == databaseEl) {
                if (treeEl.classList.contains("active"))
                    treeEl.classList.remove("active");
                else
                    treeEl.classList.add("active");
                return;
            }
            treeEl.classList.add("active");
            this.selectTreeElement(databaseEl);
            const databaseName = databaseEl.childNodes[1].textContent;
            this.onSelectDatabase(databaseName);
        };
        let firstDatabase = true;
        for (const dbContainer of dbContainers) {
            const liDatabase = createEl('li');
            const divDatabase = createEl('div');
            const dbCaret = createEl('div');
            dbCaret.classList.value = "caret";
            const dbLabel = createEl('span');
            dbLabel.innerText = dbContainer.id;
            divDatabase.title = "database";
            divDatabase.className = "clusterDatabase";
            dbLabel.style.pointerEvents = "none";
            const containerTag = createEl('span');
            containerTag.innerHTML = "tag";
            divDatabase.append(dbCaret);
            divDatabase.append(dbLabel);
            divDatabase.append(containerTag);
            liDatabase.appendChild(divDatabase);
            ulCluster.append(liDatabase);
            if (firstDatabase) {
                firstDatabase = false;
                liDatabase.classList.add("active");
                this.selectTreeElement(divDatabase);
            }
            const ulContainers = createEl('ul');
            ulContainers.onclick = (ev) => {
                ev.stopPropagation();
                const path = ev.composedPath();
                const containerEl = ClusterTree.findTreeEl(path, "clusterContainer");
                const databaseEl = containerEl.parentNode.parentNode;
                this.selectTreeElement(containerEl);
                const containerNameDiv = containerEl.children[0];
                const containerName = containerNameDiv.innerText.trim();
                const databaseName = databaseEl.childNodes[0].childNodes[1].textContent;
                this.onSelectContainer(databaseName, containerName);
            };
            liDatabase.append(ulContainers);
            for (const containerName of dbContainer.containers) {
                const liContainer = createEl('li');
                liContainer.title = "container";
                liContainer.className = "clusterContainer";
                const containerLabel = createEl('div');
                containerLabel.innerHTML = "&nbsp;" + containerName;
                liContainer.append(containerLabel);
                const containerTag = createEl('div');
                containerTag.innerHTML = "tag";
                liContainer.append(containerTag);
                ulContainers.append(liContainer);
            }
        }
        return ulCluster;
    }
    static findTreeEl(path, itemClass) {
        var _a;
        for (const el of path) {
            if ((_a = el.classList) === null || _a === void 0 ? void 0 : _a.contains(itemClass))
                return el;
        }
        return null;
    }
}
//# sourceMappingURL=components.js.map