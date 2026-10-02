class KontaApp extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.preview = null;
    this.selectedFile = null;
  }

  connectedCallback() {
    this.renderShell();
    this.loadTransactions();
  }

  renderShell() {
    this.shadowRoot.innerHTML = `
      <link rel="stylesheet" href="/styles.css">
      <div class="shell">
        <header class="topbar">
          <a class="brand" href="/" aria-label="Konta, accueil"><span class="brand-mark">K</span>Konta</a>
          <nav class="nav" aria-label="Navigation principale">
            <a href="#transactions" aria-current="page">Opérations</a>
            <a href="#import">Import CSV</a>
          </nav>
          <span class="status">Compte commun · POC</span>
        </header>
        <main>
          <div class="heading">
            <div><div class="eyebrow">Espace finances · 01</div><h1>Opérations</h1><p class="intro">Consultez les mouvements et prévisualisez un relevé avant traitement.</p></div>
          </div>
          <div class="content-grid">
            <section class="panel" id="transactions" aria-labelledby="transactions-title">
              <div class="panel-head"><h2 id="transactions-title">Dernières opérations</h2><span class="count" id="transaction-count">…</span></div>
              <div class="table-wrap"><table>
                <thead><tr><th>Date</th><th>Opération</th><th>Compte</th><th>Type</th><th style="text-align:right">Montant</th></tr></thead>
                <tbody id="transactions-body"></tbody>
              </table></div>
              <p class="empty" id="transactions-empty">Aucune opération enregistrée pour le moment. Les aperçus CSV ne sont pas enregistrés.</p>
              <p class="feedback error" id="transactions-error" role="status" hidden></p>
            </section>
            <section class="panel import-panel" id="import" aria-labelledby="import-title">
              <div class="eyebrow">Relevé bancaire</div>
              <h2 id="import-title">Prévisualiser un CSV</h2>
              <p>Le fichier est analysé en mémoire. Rien n'est ajouté à la base à cette étape.</p>
              <form id="import-form">
                <label class="file-picker"><span aria-hidden="true">＋</span><span>Choisir un relevé CSV</span><input id="csv-file" name="file" type="file" accept=".csv,text/csv" required></label>
                <div class="file-name" id="file-name">Aucun fichier choisi</div>
                <button id="preview-button" type="submit">Analyser le fichier</button>
              </form>
              <p class="feedback" id="import-feedback" role="status" aria-live="polite" hidden></p>
            </section>
          </div>
          <section class="panel preview" id="preview-panel" aria-labelledby="preview-title" hidden>
            <div class="panel-head"><h2 id="preview-title">Aperçu du fichier</h2><span class="count" id="preview-count"></span></div>
            <p class="preview-summary" id="preview-summary"></p>
            <div class="table-wrap"><table>
              <thead><tr><th>Date opération</th><th>Opération</th><th>Compte Konta</th><th>Catégorie CSV</th><th>Type</th><th style="text-align:right">Montant</th><th>Import</th></tr></thead>
              <tbody id="preview-body"></tbody>
            </table></div>
            <div class="account-setup" id="account-setup" hidden></div>
            <div class="preview-actions"><button id="commit-button" type="button" disabled>Enregistrer les opérations</button></div>
            <div class="errors" id="preview-errors" hidden></div>
          </section>
        </main>
      </div>`;

    this.shadowRoot.querySelector("#csv-file").addEventListener("change", event => {
      this.selectedFile = event.target.files[0] ?? null;
      this.shadowRoot.querySelector("#file-name").textContent = event.target.files[0]?.name ?? "Aucun fichier choisi";
    });
    this.shadowRoot.querySelector("#import-form").addEventListener("submit", event => this.previewFile(event));
    this.shadowRoot.querySelector("#preview-panel").addEventListener("click", event => this.handlePreviewAction(event));
  }

  async loadTransactions() {
    const empty = this.shadowRoot.querySelector("#transactions-empty");
    const body = this.shadowRoot.querySelector("#transactions-body");
    const error = this.shadowRoot.querySelector("#transactions-error");
    try {
      const response = await fetch("/api/transactions");
      if (!response.ok) throw new Error("Impossible de charger les opérations.");
      const transactions = await response.json();
      this.shadowRoot.querySelector("#transaction-count").textContent = `${transactions.length} opération${transactions.length === 1 ? "" : "s"}`;
      empty.hidden = transactions.length > 0;
      body.replaceChildren();
      for (const transaction of transactions) body.append(this.transactionRow(transaction));
    } catch (exception) {
      error.textContent = exception.message;
      error.hidden = false;
      this.shadowRoot.querySelector("#transaction-count").textContent = "indisponible";
    }
  }

  async previewFile(event) {
    event.preventDefault();
    if (!this.selectedFile) return;

    await this.requestPreview(this.selectedFile);
  }

  async requestPreview(file) {

    const button = this.shadowRoot.querySelector("#preview-button");
    const feedback = this.shadowRoot.querySelector("#import-feedback");
    button.disabled = true;
    button.textContent = "Analyse en cours…";
    feedback.hidden = true;
    this.shadowRoot.querySelector("#preview-panel").hidden = true;

    try {
      const formData = new FormData();
      formData.append("file", file);
      const response = await fetch("/api/imports/preview", { method: "POST", body: formData });
      const result = await response.json();
      if (!response.ok) throw new Error(result.error ?? "L'analyse du fichier a échoué.");
      this.renderPreview(result);
      feedback.className = "feedback success";
      feedback.textContent = "Aperçu prêt. Vérifiez les lignes et associez les comptes inconnus avant validation.";
      feedback.hidden = false;
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    } finally {
      button.disabled = false;
      button.textContent = "Analyser le fichier";
    }
  }

  renderPreview(preview) {
    this.preview = preview;
    const panel = this.shadowRoot.querySelector("#preview-panel");
    const body = this.shadowRoot.querySelector("#preview-body");
    body.replaceChildren();
    for (const transaction of preview.transactions) body.append(this.previewRow(transaction));

    this.shadowRoot.querySelector("#preview-count").textContent = `${preview.transactions.length} ligne${preview.transactions.length === 1 ? "" : "s"} valide${preview.transactions.length === 1 ? "" : "s"}`;
    const duplicateCount = preview.transactions.filter(transaction => transaction.isDuplicate).length;
    this.shadowRoot.querySelector("#preview-summary").textContent = `${preview.errors.length} erreur${preview.errors.length === 1 ? "" : "s"} · ${duplicateCount} doublon${duplicateCount === 1 ? "" : "s"} détecté${duplicateCount === 1 ? "" : "s"} · Le signe du montant détermine le type.`;
    this.renderAccountSetup(preview.unmappedAccounts);
    const commitButton = this.shadowRoot.querySelector("#commit-button");
    commitButton.disabled = preview.transactions.length === 0 || preview.unmappedAccounts.length > 0;
    const errors = this.shadowRoot.querySelector("#preview-errors");
    errors.replaceChildren();
    errors.hidden = preview.errors.length === 0;
    if (preview.errors.length > 0) {
      const title = document.createElement("strong");
      title.textContent = "Lignes à corriger";
      const list = document.createElement("ul");
      for (const error of preview.errors) {
        const item = document.createElement("li");
        item.textContent = `Ligne ${error.rowNumber} : ${error.message}`;
        list.append(item);
      }
      errors.append(title, list);
    }
    panel.hidden = false;
    panel.scrollIntoView({ behavior: "smooth", block: "start" });
  }

  renderAccountSetup(accounts) {
    const container = this.shadowRoot.querySelector("#account-setup");
    container.replaceChildren();
    container.hidden = accounts.length === 0;
    if (accounts.length === 0) return;

    const title = document.createElement("h3");
    title.textContent = "Associer les comptes du relevé";
    const description = document.createElement("p");
    description.textContent = "Chaque numéro SG doit être associé une seule fois avant l'import.";
    container.append(title, description);

    for (const account of accounts) {
      const row = document.createElement("div");
      row.className = "account-map";
      row.dataset.accountNumber = account.accountNumber;

      const bank = document.createElement("div");
      bank.className = "account-map-source";
      const bankLabel = document.createElement("strong");
      bankLabel.textContent = account.bankLabel;
      const bankNumber = document.createElement("span");
      bankNumber.textContent = account.accountNumber;
      bank.append(bankLabel, bankNumber);

      const nameLabel = document.createElement("label");
      nameLabel.textContent = "Nom dans Konta";
      const name = document.createElement("input");
      name.required = true;
      name.maxLength = 100;
      name.value = account.bankLabel;
      name.dataset.accountName = "";
      nameLabel.append(name);

      const kindLabel = document.createElement("label");
      kindLabel.textContent = "Type";
      const kind = document.createElement("select");
      kind.dataset.accountKind = "";
      for (const [value, label] of [["Current", "Compte courant"], ["Savings", "Épargne"], ["Other", "Autre"]]) {
        const option = document.createElement("option");
        option.value = value;
        option.textContent = label;
        kind.append(option);
      }
      kindLabel.append(kind);

      const createButton = document.createElement("button");
      createButton.type = "button";
      createButton.textContent = "Associer ce compte";
      createButton.dataset.createAccount = "";
      row.append(bank, nameLabel, kindLabel, createButton);
      container.append(row);
    }
  }

  async handlePreviewAction(event) {
    const createButton = event.target.closest("[data-create-account]");
    if (createButton) {
      const row = createButton.closest(".account-map");
      const name = row.querySelector("[data-account-name]").value.trim();
      if (!name) return;
      createButton.disabled = true;
      try {
        const response = await fetch("/api/accounts", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name,
            bankAccountNumber: row.dataset.accountNumber,
            bankLabel: row.querySelector(".account-map-source strong").textContent,
            kind: row.querySelector("[data-account-kind]").value
          })
        });
        const result = await response.json();
        if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible d'associer ce compte.");
        await this.requestPreview(this.selectedFile);
      } catch (exception) {
        const feedback = this.shadowRoot.querySelector("#import-feedback");
        feedback.className = "feedback error";
        feedback.textContent = exception.message;
        feedback.hidden = false;
        createButton.disabled = false;
      }
      return;
    }

    if (event.target.closest("#commit-button")) {
      await this.commitImport();
    }
  }

  async commitImport() {
    if (!this.selectedFile || !this.preview || this.preview.unmappedAccounts.length > 0) return;
    const button = this.shadowRoot.querySelector("#commit-button");
    const feedback = this.shadowRoot.querySelector("#import-feedback");
    button.disabled = true;
    button.textContent = "Enregistrement…";
    try {
      const formData = new FormData();
      formData.append("file", this.selectedFile);
      const response = await fetch("/api/imports/commit", { method: "POST", body: formData });
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "L'import a échoué.");
      feedback.className = "feedback success";
      feedback.textContent = `${result.importedCount} opération${result.importedCount === 1 ? "" : "s"} ajoutée${result.importedCount === 1 ? "" : "s"}, ${result.duplicateCount} doublon${result.duplicateCount === 1 ? "" : "s"} ignoré${result.duplicateCount === 1 ? "" : "s"}, ${result.errors.length} erreur${result.errors.length === 1 ? "" : "s"}.`;
      feedback.hidden = false;
      await this.loadTransactions();
      await this.requestPreview(this.selectedFile);
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
      button.disabled = false;
    } finally {
      button.textContent = "Enregistrer les opérations";
    }
  }

  transactionRow(transaction) {
    const row = document.createElement("tr");
    row.append(
      this.cell(this.formatDate(transaction.date), "date"),
      this.cell(transaction.label),
      this.cell(transaction.accountName ?? transaction.accountLabel ?? "—"),
      this.typeCell(transaction.type),
      this.cell(this.formatAmount(transaction.amount, transaction.type), "amount")
    );
    return row;
  }

  previewRow(transaction) {
    const row = document.createElement("tr");
    if (transaction.isDuplicate) row.className = "duplicate-row";
    const category = [transaction.categoryName, transaction.subcategoryName].filter(Boolean).join(" / ") || "Non catégorisé";
    const accountName = transaction.accountName ?? `À associer · ${transaction.accountLabel}`;
    row.append(
      this.cell(this.formatDate(transaction.transactionDate), "date"),
      this.cell(transaction.label),
      this.cell(`${accountName} · ${transaction.accountNumber}`),
      this.cell(category, "category"),
      this.typeCell(transaction.type),
      this.cell(this.formatAmount(transaction.amount, transaction.type), "amount"),
      this.cell(transaction.isDuplicate ? "Déjà importée" : "À importer", "import-state")
    );
    return row;
  }

  cell(text, className = "") {
    const cell = document.createElement("td");
    cell.className = className;
    cell.textContent = text;
    return cell;
  }

  typeCell(type) {
    const cell = document.createElement("td");
    const badge = document.createElement("span");
    const isExpense = type === 0 || type === "Expense";
    badge.className = `type ${isExpense ? "expense" : "income"}`;
    badge.textContent = isExpense ? "Dépense" : "Revenu";
    cell.append(badge);
    return cell;
  }

  formatDate(value) {
    return new Intl.DateTimeFormat("fr-FR").format(new Date(`${value}T00:00:00`));
  }

  formatAmount(amount, type) {
    const isExpense = type === 0 || type === "Expense";
    const formatted = new Intl.NumberFormat("fr-FR", { style: "currency", currency: "EUR" }).format(amount);
    return isExpense ? `− ${formatted}` : `+ ${formatted}`;
  }
}

customElements.define("konta-app", KontaApp);