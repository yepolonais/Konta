class KontaApp extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.preview = null;
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
              <thead><tr><th>Date opération</th><th>Opération</th><th>Compte</th><th>Catégorie CSV</th><th>Type</th><th style="text-align:right">Montant</th></tr></thead>
              <tbody id="preview-body"></tbody>
            </table></div>
            <div class="errors" id="preview-errors" hidden></div>
          </section>
        </main>
      </div>`;

    this.shadowRoot.querySelector("#csv-file").addEventListener("change", event => {
      this.shadowRoot.querySelector("#file-name").textContent = event.target.files[0]?.name ?? "Aucun fichier choisi";
    });
    this.shadowRoot.querySelector("#import-form").addEventListener("submit", event => this.previewFile(event));
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
      for (const transaction of transactions) body.append(this.transactionRow(transaction));
    } catch (exception) {
      error.textContent = exception.message;
      error.hidden = false;
      this.shadowRoot.querySelector("#transaction-count").textContent = "indisponible";
    }
  }

  async previewFile(event) {
    event.preventDefault();
    const input = this.shadowRoot.querySelector("#csv-file");
    const file = input.files[0];
    if (!file) return;

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
      feedback.textContent = "Aperçu prêt. Aucune opération n'a été enregistrée.";
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
    const panel = this.shadowRoot.querySelector("#preview-panel");
    const body = this.shadowRoot.querySelector("#preview-body");
    body.replaceChildren();
    for (const transaction of preview.transactions) body.append(this.previewRow(transaction));

    this.shadowRoot.querySelector("#preview-count").textContent = `${preview.transactions.length} ligne${preview.transactions.length === 1 ? "" : "s"} valide${preview.transactions.length === 1 ? "" : "s"}`;
    this.shadowRoot.querySelector("#preview-summary").textContent = `${preview.errors.length} erreur${preview.errors.length === 1 ? "" : "s"} · Dépenses et revenus reconnus à partir du signe du montant.`;
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

  transactionRow(transaction) {
    const row = document.createElement("tr");
    row.append(
      this.cell(this.formatDate(transaction.date), "date"),
      this.cell(transaction.label),
      this.cell(transaction.accountLabel ?? "—"),
      this.typeCell(transaction.type),
      this.cell(this.formatAmount(transaction.amount, transaction.type), "amount")
    );
    return row;
  }

  previewRow(transaction) {
    const row = document.createElement("tr");
    const category = [transaction.categoryName, transaction.subcategoryName].filter(Boolean).join(" / ") || "Non catégorisé";
    row.append(
      this.cell(this.formatDate(transaction.transactionDate), "date"),
      this.cell(transaction.label),
      this.cell(`${transaction.accountLabel} · ${transaction.accountNumber}`),
      this.cell(category, "category"),
      this.typeCell(transaction.type),
      this.cell(this.formatAmount(transaction.amount, transaction.type), "amount")
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