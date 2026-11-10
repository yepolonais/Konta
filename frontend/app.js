const OPERATION_TYPES = {
  Card: "Carte",
  TransferReceived: "Virement reçu",
  TransferSent: "Virement émis",
  DirectDebit: "Prélèvement",
  Loan: "Prêt",
  BankFee: "Frais bancaires"
};

const OPERATION_TYPE_ICONS = {
  Card: ["M3 6h18v12H3z", "M3 10h18", "M6 15h4"],
  TransferReceived: ["M12 4v12", "M7 11l5 5 5-5", "M5 20h14"],
  TransferSent: ["M12 16V4", "M7 9l5-5 5 5", "M5 20h14"],
  DirectDebit: ["M4 12a8 8 0 0 1 14-5", "M18 3v4h-4", "M20 12a8 8 0 0 1-14 5", "M6 21v-4h4"],
  Loan: ["M4 11l8-7 8 7", "M6 10v10h12V10", "M10 20v-5h4v5"],
  BankFee: ["M3 9l9-5 9 5", "M5 9v8", "M9 9v8", "M15 9v8", "M19 9v8", "M3 20h18"],
  Any: ["M12 5v14", "M5 8.5l14 7", "M19 8.5l-14 7"]
};

const BUDGET_GROUPS = [
  { type: "Income", label: "Revenus", emptyLabel: "Aucun poste de revenus", typeLabel: "Revenu" },
  { type: "Expense", expenseKind: "Fixed", label: "Charges fixes", emptyLabel: "Aucune charge fixe", typeLabel: "Dépense" },
  { type: "Expense", expenseKind: "Variable", label: "Dépenses variables", emptyLabel: "Aucune dépense variable", typeLabel: "Dépense" },
  { type: "Savings", label: "Épargne planifiée", emptyLabel: "Aucune épargne planifiée", typeLabel: "Épargne" }
];

class KontaApp extends HTMLElement {
  constructor() {
    super();
    this.attachShadow({ mode: "open" });
    this.preview = null;
    this.selectedFile = null;
    this.accountMappings = {};
    this.categories = [];
    this.accounts = [];
    this.categoryRules = [];
    this.editingRuleId = null;
    this.editingCategoryId = null;
    this.transactions = [];
    this.annualBudget = null;
    this.annualBudgetCategories = [];
    this.mappingPosts = [];
    this.mappingOrphans = [];
    this.annualBudgetMonth = null;
    this.draggedBudgetPostId = null;
    this.selectedBudgetMonth = new Date().getMonth() + 1;
    this.selectedMonthlyYear = new Date().getFullYear();
    this.monthlyBudget = null;
  }

  connectedCallback() {
    this.renderShell();
    this.setTheme(localStorage.getItem("konta-theme") === "dark" ? "dark" : "light");
    this.setAmbiance(localStorage.getItem("konta-ambiance") === "cherry" ? "cherry" : "plain");
    this.handlePageNavigation = () => {
      this.showPage(window.location.hash.slice(1) || "budget");
    };
    window.addEventListener("hashchange", this.handlePageNavigation);
    window.addEventListener("popstate", this.handlePageNavigation);
    this.handleBudgetResize = () => {
      if (this.currentPage === "budget") this.renderAnnualBudget();
    };
    window.addEventListener("resize", this.handleBudgetResize);
    this.showPage(window.location.hash.slice(1) || "budget");
    this.loadImportSettings();
    this.loadAnnualBudget();
    this.loadTransactions();
    this.phase2DataPromise = this.loadPhase2Data();
  }

  disconnectedCallback() {
    window.removeEventListener("hashchange", this.handlePageNavigation);
    window.removeEventListener("popstate", this.handlePageNavigation);
    window.removeEventListener("resize", this.handleBudgetResize);
  }

  renderShell() {
    this.shadowRoot.innerHTML = `
      <link rel="stylesheet" href="/styles.css">
      <div class="shell">
        <header class="topbar">
          <a class="brand" href="/" aria-label="Konta, accueil"><span class="brand-mark">K</span>Konta</a>
          <nav class="nav" aria-label="Navigation principale">
            <a class="nav-plan" href="#budget" aria-current="page"><span class="nav-plan-kicker">Prévisionnel</span>Budget</a>
            <span class="nav-separator" aria-hidden="true"></span>
            <a href="#monthly">Prévu vs réalisé</a>
            <a href="#summary">Synthèses</a>
            <a href="#mapping">Rapprochement<span class="nav-badge" id="mapping-badge" hidden></span></a>
            <a href="#category-rules">Catégorisation</a>
            <a href="#import">Import CSV</a>
          </nav>
          <div class="appearance-controls" aria-label="Apparence">
            <button class="theme-toggle" type="button" aria-label="Activer le mode sombre" aria-pressed="false">
              <span class="theme-toggle-icon" aria-hidden="true">☾</span><span class="theme-toggle-label">Sombre</span>
            </button>
            <button class="ambiance-toggle secondary-button" type="button" aria-label="Activer l’ambiance cerisier" aria-pressed="false">
              <span class="ambiance-toggle-icon" aria-hidden="true">🌸</span><span class="ambiance-toggle-label">Cerisier</span>
            </button>
          </div>
        </header>
        <main>
          <div class="heading">
            <div><div class="eyebrow">Espace finances · 01</div><h1>Budget</h1><p class="intro">Planifiez les revenus, dépenses et montants mis de côté sur l’année.</p></div>
          </div>
          <section class="panel annual-budget-panel" id="budget" data-page="budget" aria-labelledby="annual-budget-title">
            <div class="panel-head annual-budget-heading">
              <div><span class="section-kicker">Prévision annuelle</span><h2 id="annual-budget-title">Budget du foyer</h2></div>
              <div class="budget-year-label" role="group" aria-label="Année du budget">
                <button type="button" class="secondary-button" id="annual-budget-year-prev" aria-label="Année précédente">‹</button>
                <input id="annual-budget-year" type="number" min="1" max="9998" aria-label="Année" required>
                <button type="button" class="secondary-button" id="annual-budget-year-next" aria-label="Année suivante">›</button>
              </div>
            </div>
            <div class="annual-budget-totals" id="annual-budget-totals" aria-live="polite"></div>
            <form class="annual-budget-form" id="annual-budget-form">
              <label>Nom du poste<input id="annual-budget-post-name" maxlength="100" placeholder="Ex. Loyer, salaires, vacances" required></label>
              <label>Type<select id="annual-budget-post-type">
                <option value="Income">Revenu</option>
                <option value="Expense">Dépense</option>
                <option value="Savings">Épargne planifiée</option>
              </select></label>
              <label id="annual-budget-expense-kind-field" hidden>Catégorie de dépense<select id="annual-budget-expense-kind">
                <option value="Variable">Dépense variable</option>
                <option value="Fixed">Charge fixe</option>
              </select></label>
              <button type="submit">Ajouter un poste</button>
            </form>
            <p class="feedback" id="annual-budget-feedback" role="status" aria-live="polite" hidden></p>
            <div class="table-wrap annual-budget-table-wrap">
              <table class="annual-budget-table">
                <thead id="annual-budget-head"></thead>
                <tbody id="annual-budget-body"></tbody>
                <tfoot id="annual-budget-foot"></tfoot>
              </table>
            </div>
            <p class="empty" id="annual-budget-empty" hidden>Ajoutez vos premiers postes prévisionnels pour construire le budget de l’année.</p>
          </section>
          <section class="panel budget-monthly-panel" id="monthly" data-page="monthly" aria-labelledby="budget-monthly-title" hidden>
            <section class="budget-monthly-review" aria-labelledby="budget-monthly-title">
              <div class="budget-monthly-heading">
                <div><span class="section-kicker">Comparatif mensuel</span><div class="rules-title-group"><h3 id="budget-monthly-title">Prévu et réalisé</h3>
                  <span class="info-tip">
                    <button type="button" class="info-tip-button" aria-label="À propos du calcul du réalisé" aria-describedby="monthly-info"><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"></circle><path d="M12 11v5"></path><path d="M12 8h.01"></path></svg></button>
                    <span class="info-tip-bubble" id="monthly-info" role="tooltip">Les virements entre vos comptes ne sont comptés ni dans les revenus ni dans les dépenses. Le réalisé d’une enveloppe d’épargne correspond aux versements sortants des catégories rattachées.</span>
                  </span></div></div>
                <div class="budget-monthly-filters">
                  <div class="budget-year-label" role="group" aria-label="Année du suivi">
                    <button type="button" class="secondary-button" id="monthly-review-year-prev" aria-label="Année précédente">‹</button>
                    <input id="monthly-review-year" type="number" min="1" max="9998" aria-label="Année du suivi" required>
                    <button type="button" class="secondary-button" id="monthly-review-year-next" aria-label="Année suivante">›</button>
                  </div>
                  <div class="budget-year-label" role="group" aria-label="Mois du suivi">
                    <button type="button" class="secondary-button" id="monthly-review-month-prev" aria-label="Mois précédent">‹</button>
                    <select id="monthly-review-month" aria-label="Mois"></select>
                    <button type="button" class="secondary-button" id="monthly-review-month-next" aria-label="Mois suivant">›</button>
                  </div>
                </div>
                <p id="budget-monthly-period"></p>
              </div>
              <div class="budget-monthly-totals" id="budget-monthly-totals"></div>
              <p class="feedback error" id="budget-month-feedback" role="status" aria-live="polite" hidden></p>
              <div class="table-wrap">
                <table class="budget-monthly-table">
                  <thead><tr><th>Type</th><th>Poste</th><th>Prévu</th><th>Réalisé</th><th>Écart réalisé − prévu</th></tr></thead>
                  <tbody id="budget-monthly-body"></tbody>
                </table>
              </div>
              <div class="budget-unassigned" id="budget-unassigned" hidden></div>
            </section>
          </section>
          <section class="panel summary-panel" id="summary" data-page="summary" aria-labelledby="summary-title">
            <div class="panel-head"><div><span class="section-kicker">Analyse financière</span><h2 id="summary-title">Vue d’ensemble</h2></div><span class="count summary-count" id="summary-count"></span></div>
            <form class="summary-filters" id="summary-form">
              <div class="summary-filter-heading"><strong>Choisir une période</strong><span>Affinez les résultats par compte ou catégorie.</span></div>
              <label>Année<input id="summary-year" type="number" min="1" max="9998" required></label>
              <label>Mois<select id="summary-month"><option value="">Année complète</option></select></label>
              <label>Compte<select id="summary-account"><option value="">Tous les comptes</option></select></label>
              <label>Catégorie<select id="summary-category"><option value="">Toutes les catégories</option></select></label>
              <button class="summary-submit" type="submit"><span aria-hidden="true">↻</span> Actualiser</button>
            </form>
            <p class="feedback error" id="summary-error" role="status" hidden></p>
            <div id="summary-results" hidden>
              <div class="summary-result-heading"><div><span class="section-kicker">En un coup d’œil</span><h3>Résultat de la période</h3></div><p id="summary-period"></p></div>
              <div class="summary-totals" id="summary-totals"></div>
              <div class="summary-table-heading"><div><h3>Dépenses et revenus par catégorie</h3><p>Ouvrez une catégorie pour consulter les opérations correspondantes.</p></div></div>
              <div class="table-wrap"><table class="summary-table">
                <thead><tr><th data-sort-key="name" aria-sort="none"><button type="button" class="text-button sort-button" data-sort="name" title="Trier par catégorie">Catégorie <span class="sort-caret" aria-hidden="true">⇅</span></button></th><th>Revenus</th><th>Dépenses</th><th data-sort-key="net" aria-sort="none"><button type="button" class="text-button sort-button" data-sort="net" title="Trier par solde net">Solde net <span class="sort-caret" aria-hidden="true">⇅</span></button></th><th></th></tr></thead>
                <tbody id="summary-categories"></tbody>
              </table></div>
            </div>
          </section>
          <div class="content-grid">
            <section class="panel import-source-panel" data-page="import" aria-labelledby="import-source-title">
              <div class="eyebrow">Relevé bancaire</div>
              <h2 id="import-source-title">Récupérer un relevé</h2>
              <p>Ces paramètres sont enregistrés dans la base locale de Konta.</p>
              <form id="import-source-form">
                <label for="import-bank-identifier">Mon identifiant<input id="import-bank-identifier" name="bankIdentifier" maxlength="100" required></label>
                <label for="import-source-url">Adresse de la banque
                  <span class="import-source-controls">
                    <input id="import-source-url" name="bankUrl" type="url" maxlength="2048" required>
                    <button id="import-source-go" type="button">Go</button>
                  </span>
                </label>
                <button id="import-source-save" type="submit">Enregistrer</button>
              </form>
              <p class="feedback" id="import-source-feedback" role="status" aria-live="polite" hidden></p>
            </section>
            <section class="panel" id="transactions" data-page="transactions" aria-labelledby="transactions-title">
              <div class="panel-head"><h2 id="transactions-title">Dernières opérations</h2><span class="count" id="transaction-count">…</span></div>
              <div class="table-wrap"><table>
                <thead><tr><th>Date</th><th>Opération</th><th>Compte</th><th>Type</th><th style="text-align:right">Montant</th></tr></thead>
                <tbody id="transactions-body"></tbody>
              </table></div>
              <p class="empty" id="transactions-empty">Aucune opération enregistrée pour le moment. Les aperçus CSV ne sont pas enregistrés.</p>
              <p class="feedback error" id="transactions-error" role="status" hidden></p>
            </section>
            <section class="panel import-panel" id="import" data-page="import" aria-labelledby="import-title">
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
          <section class="panel preview" id="preview-panel" data-page="import" aria-labelledby="preview-title" hidden>
            <div class="panel-head"><h2 id="preview-title">Aperçu du fichier</h2><span class="count" id="preview-count"></span></div>
            <p class="preview-summary" id="preview-summary"></p>
            <div class="preview-toolbar">
              <label class="preview-filter"><input id="hide-duplicates" type="checkbox" checked> Masquer les doublons détectés</label>
              <label class="preview-filter"><input id="hide-categorized" type="checkbox"> Masquer les opérations catégorisées</label>
            </div>
            <div class="table-wrap"><table class="preview-table">
              <thead><tr><th>Date</th><th>Détails</th><th>Compte Konta</th><th>Catégorie / suggestion</th><th style="text-align:right">Montant (€)</th><th>Inclure</th></tr></thead>
              <tbody id="preview-body"></tbody>
            </table></div>
            <div class="account-setup" id="account-setup" hidden></div>
            <div class="preview-actions"><button id="commit-button" type="button" disabled>Enregistrer les opérations</button></div>
            <div class="errors" id="preview-errors" hidden></div>
          </section>
          <section class="panel balance-panel" id="account-balance" data-page="import" aria-labelledby="balance-title">
            <div class="eyebrow">Après l’import</div>
            <h2 id="balance-title">Solde constaté d’un compte</h2>
            <p>Facultatif : le solde est repris automatiquement de l’en-tête des relevés CSV récents. Sinon, indiquez le solde réel de votre compte juste après une opération (par défaut, la dernière).</p>
            <form class="balance-form" id="balance-form">
              <label>Compte<select id="balance-account" required></select></label>
              <label>Opération de référence<select id="balance-transaction" required></select></label>
              <label>Solde juste après l’opération (€)<input id="reference-balance" type="number" step="0.01" required></label>
              <button type="submit">Définir le solde</button>
            </form>
            <p id="balance-result" class="balance-result" role="status"></p>
            <p class="feedback" id="balance-feedback" role="status" aria-live="polite" hidden></p>
          </section>
          <section class="panel mapping-panel" id="mapping" data-page="mapping" aria-labelledby="mapping-title">
            <div class="panel-head"><h2 id="mapping-title">Rapprochement catégories / postes</h2></div>
            <p class="rules-intro">Une catégorie réelle alimente au plus un poste budgétaire. Une catégorie sans poste est « à rattacher » : ses opérations ne sont pas rapprochées du budget.</p>
            <p class="feedback" id="mapping-feedback" role="status" aria-live="polite" hidden></p>
            <div class="mapping-orphans" id="mapping-orphans"></div>
            <div class="mapping-posts" id="mapping-posts"></div>
          </section>
          <section class="panel rules-panel" id="category-rules" data-page="category-rules" aria-labelledby="rules-title">
            <div class="panel-head">
              <div class="rules-title-group">
                <h2 id="rules-title">Associations motif → catégorie</h2>
                <span class="info-tip">
                  <button type="button" class="info-tip-button" aria-label="À propos des associations" aria-describedby="rules-info"><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="9"></circle><path d="M12 11v5"></path><path d="M12 8h.01"></path></svg></button>
                  <span class="info-tip-bubble" id="rules-info" role="tooltip">Une association dirige automatiquement vers une catégorie les opérations dont le détail contient un motif (commerçant d’un paiement carte, émetteur d’un virement ou d’un prélèvement…). Une association limitée à un type d’opération est prioritaire, puis le motif le plus long.</span>
                </span>
              </div>
              <span class="count" id="rules-count"></span>
            </div>
            <form class="rule-form" id="category-rule-form">
              <label>Motif à rechercher<input id="rule-pattern" name="pattern" maxlength="200" required></label>
              <label>Type d’opération<select id="rule-operation-type" name="operationType">
                <option value="">Tous les types</option>
                ${Object.entries(OPERATION_TYPES).map(([value, label]) => `<option value="${value}">${label}</option>`).join("")}
              </select></label>
              <label>Catégorie<select id="rule-category" name="categoryId" required></select></label>
              <div class="rule-form-actions"><button id="rule-submit" type="submit">Associer</button><button id="rule-cancel" type="button" class="secondary-button" hidden>Annuler</button></div>
            </form>
            <form class="category-form" id="category-form">
              <label>Nouvelle catégorie<input id="new-category-name" maxlength="100" required></label>
              <button id="category-submit" type="submit" class="secondary-button">Ajouter une catégorie</button>
              <button id="category-cancel" type="button" class="secondary-button" hidden>Annuler</button>
            </form>
            <div class="category-list" id="category-list" aria-label="Catégories actives"></div>
            <p class="feedback" id="rules-feedback" role="status" aria-live="polite" hidden></p>
          </section>
        </main>
      </div>`;

    this.shadowRoot.querySelector("#csv-file").addEventListener("change", event => {
      this.selectedFile = event.target.files[0] ?? null;
      this.shadowRoot.querySelector("#file-name").textContent = event.target.files[0]?.name ?? "Aucun fichier choisi";
    });
    this.shadowRoot.querySelector("#import-source-form").addEventListener("submit", event => this.saveImportSettings(event));
    this.shadowRoot.querySelector("#import-source-go").addEventListener("click", () => {
      const input = this.shadowRoot.querySelector("#import-source-url");
      if (!input.reportValidity()) return;

      let destination;
      try {
        destination = new URL(input.value.trim());
      } catch {
        input.setCustomValidity("Saisissez une adresse Web valide commençant par https:// ou http://.");
        input.reportValidity();
        return;
      }

      if (!["http:", "https:"].includes(destination.protocol) || destination.username || destination.password) {
        input.setCustomValidity("Utilisez une adresse Web http:// ou https:// sans identifiant.");
        input.reportValidity();
        return;
      }

      input.setCustomValidity("");
      window.open(destination.href, "_blank", "noopener,noreferrer");
    });
    this.shadowRoot.querySelector("#import-source-url").addEventListener("input", event => {
      event.target.setCustomValidity("");
    });
    this.shadowRoot.querySelector("#import-bank-identifier").addEventListener("input", event => {
      event.target.setCustomValidity("");
    });
    this.shadowRoot.querySelector(".nav").addEventListener("click", event => {
      const link = event.target.closest("a[href^='#']");
      if (!link) return;
      event.preventDefault();
      const page = link.hash.slice(1);
      if (page === (window.location.hash.slice(1) || "transactions")) {
        this.showPage(page);
      } else {
        window.history.pushState(null, "", link.hash);
        this.showPage(page);
      }
    });
    this.shadowRoot.querySelector(".theme-toggle").addEventListener("click", () => {
      this.setTheme(this.dataset.theme === "dark" ? "light" : "dark", true);
    });
    this.shadowRoot.querySelector(".ambiance-toggle").addEventListener("click", () => {
      this.setAmbiance(this.dataset.ambiance === "cherry" ? "plain" : "cherry", true);
    });
    this.shadowRoot.querySelector("#import-form").addEventListener("submit", event => this.previewFile(event));
    this.shadowRoot.querySelector("#preview-panel").addEventListener("click", event => this.handlePreviewAction(event));
    this.shadowRoot.querySelector("#summary-form").addEventListener("submit", event => {
      event.preventDefault();
      this.loadSummary();
    });
    this.shadowRoot.querySelector("#annual-budget-form").addEventListener("submit", event => this.createAnnualBudgetPost(event));
    this.shadowRoot.querySelector("#annual-budget-post-type").addEventListener("change", event => {
      this.shadowRoot.querySelector("#annual-budget-expense-kind-field").hidden = event.target.value !== "Expense";
    });
    this.shadowRoot.querySelector("#annual-budget-year").addEventListener("change", () => this.loadAnnualBudget());
    for (const [selector, step] of [["#annual-budget-year-prev", -1], ["#annual-budget-year-next", 1]]) {
      this.shadowRoot.querySelector(selector).addEventListener("click", () => {
        const input = this.shadowRoot.querySelector("#annual-budget-year");
        const year = Number(input.value) + step;
        if (year < 1 || year > 9998) return;
        input.value = year;
        this.loadAnnualBudget();
      });
    }
    this.shadowRoot.querySelector("#monthly-review-month").addEventListener("change", event => {
      this.selectedBudgetMonth = Number(event.target.value);
      this.renderAnnualBudget();
      this.loadAnnualBudgetMonth();
    });
    const monthlyYearInput = this.shadowRoot.querySelector("#monthly-review-year");
    const changeMonthlyYear = year => {
      if (!Number.isInteger(year) || year < 1 || year > 9998) {
        monthlyYearInput.value = this.selectedMonthlyYear;
        return;
      }
      this.selectedMonthlyYear = year;
      monthlyYearInput.value = year;
      this.updateMonthlyTitle();
      this.loadAnnualBudgetMonth();
    };
    monthlyYearInput.addEventListener("change", () => changeMonthlyYear(Number(monthlyYearInput.value)));
    for (const [selector, step] of [["#monthly-review-year-prev", -1], ["#monthly-review-year-next", 1]]) {
      this.shadowRoot.querySelector(selector).addEventListener("click", () => changeMonthlyYear(this.selectedMonthlyYear + step));
    }
    for (const [selector, step] of [["#monthly-review-month-prev", -1], ["#monthly-review-month-next", 1]]) {
      this.shadowRoot.querySelector(selector).addEventListener("click", () => {
        let month = this.selectedBudgetMonth + step;
        let targetYear = this.selectedMonthlyYear;
        if (month < 1) { month = 12; targetYear--; }
        if (month > 12) { month = 1; targetYear++; }
        if (targetYear < 1 || targetYear > 9998) return;
        this.selectedBudgetMonth = month;
        this.shadowRoot.querySelector("#monthly-review-month").value = String(month);
        if (targetYear !== this.selectedMonthlyYear) {
          changeMonthlyYear(targetYear);
        } else {
          this.renderAnnualBudget();
          this.loadAnnualBudgetMonth();
        }
      });
    }
    this.shadowRoot.querySelector("#budget-monthly-body").addEventListener("change", event => this.handleAnnualBudgetChange(event));
    this.shadowRoot.querySelector("#annual-budget-body").addEventListener("change", event => this.handleAnnualBudgetChange(event));
    this.shadowRoot.querySelector("#annual-budget-body").addEventListener("focusin", event => {
      const input = event.target.closest("[data-month-amount]");
      if (input) input.closest("[data-post-id]").dataset.sourceMonth = input.dataset.month;
    });
    const annualBudgetBody = this.shadowRoot.querySelector("#annual-budget-body");
    annualBudgetBody.addEventListener("dragstart", event => this.handleBudgetPostDragStart(event));
    annualBudgetBody.addEventListener("dragover", event => this.handleBudgetPostDragOver(event));
    annualBudgetBody.addEventListener("dragleave", event => this.handleBudgetPostDragLeave(event));
    annualBudgetBody.addEventListener("drop", event => this.handleBudgetPostDrop(event));
    annualBudgetBody.addEventListener("dragend", () => this.clearBudgetPostDragState());
    annualBudgetBody.addEventListener("keydown", event => this.handleBudgetPostMoveKeydown(event));
    // Garde le focus sur le champ pour que la flèche reste visible et que la valeur saisie soit recopiée.
    this.shadowRoot.querySelector("#annual-budget-body").addEventListener("mousedown", event => {
      if (event.target.closest(".budget-inline-copy")) event.preventDefault();
    });
    this.shadowRoot.querySelector("#annual-budget-body").addEventListener("click", event => this.handleAnnualBudgetAction(event));
    this.shadowRoot.querySelector("#category-rule-form").addEventListener("submit", event => this.saveCategoryRule(event));
    this.shadowRoot.querySelector("#rule-cancel").addEventListener("click", () => this.resetRuleForm());
    this.shadowRoot.querySelector("#category-form").addEventListener("submit", event => this.createCategory(event));
    this.shadowRoot.querySelector("#category-cancel").addEventListener("click", () => this.resetCategoryForm());
    this.shadowRoot.querySelector("#mapping").addEventListener("click", event => this.handleMappingClick(event));
    this.shadowRoot.querySelector("#category-list").addEventListener("click", event => {
      this.handleRuleAction(event);
      this.handleCategoryAction(event);
    });
    this.shadowRoot.querySelector(".summary-table thead").addEventListener("click", event => {
      const button = event.target.closest("[data-sort]");
      if (!button) return;
      const key = button.dataset.sort;
      const current = this.summarySort?.key === key ? this.summarySort.direction : null;
      this.summarySort = { asc: { key, direction: "desc" }, desc: null }[current] ?? { key, direction: "asc" };
      if (this.summary) this.renderSummary(this.summary);
    });
    this.shadowRoot.querySelector("#balance-form").addEventListener("submit", event => this.setBalanceReference(event));
    this.shadowRoot.querySelector("#balance-account").addEventListener("change", () => {
      this.renderBalanceTransactions(true);
      this.loadBalance();
    });
    this.shadowRoot.querySelector("#preview-body").addEventListener("change", event => {
      if (event.target.matches("[data-category]")) {
        this.updateReviewCategory(event.target);
        this.syncMatchingReviewRows(event.target.closest("tr"));
        this.refreshPendingCategoryOptions();
      }
      if (event.target.matches("[data-remember-rule], [data-rule-pattern]")) {
        this.syncMatchingReviewRows(event.target.closest("tr"));
      }
      if (event.target.matches("[data-new-category]")) {
        this.syncMatchingReviewRows(event.target.closest("tr"));
        this.refreshPendingCategoryOptions();
      }
      if (event.target.matches("[data-new-category]")) {
        setTimeout(() => this.updatePreviewVisibility(), 0);
      } else if (event.target.matches("[data-category], [data-remember-rule], [data-rule-pattern]")) {
        this.updatePreviewVisibility();
      }
    });
    this.shadowRoot.querySelector("#preview-body").addEventListener("input", event => {
      if (event.target.matches("[data-amount]")) this.updateReviewAmount(event.target);
    });
    this.shadowRoot.querySelector("#preview-body").addEventListener("focusout", event => {
      const row = event.target.closest("tr");
      if (row && !row.contains(event.relatedTarget)) {
        setTimeout(() => this.updatePreviewVisibility(), 0);
      }
    });
    this.shadowRoot.querySelector("#hide-duplicates").addEventListener("change", () => this.updatePreviewVisibility());
    this.shadowRoot.querySelector("#hide-categorized").addEventListener("change", () => this.updatePreviewVisibility());

    const year = this.shadowRoot.querySelector("#summary-year");
    year.value = new Date().getFullYear();
    const annualYear = this.shadowRoot.querySelector("#annual-budget-year");
    annualYear.value = year.value;
    this.shadowRoot.querySelector("#monthly-review-year").value = this.selectedMonthlyYear;
    const month = this.shadowRoot.querySelector("#summary-month");
    const monthlyMonth = this.shadowRoot.querySelector("#monthly-review-month");
    for (let value = 1; value <= 12; value++) {
      const option = document.createElement("option");
      option.value = value;
      const monthName = new Intl.DateTimeFormat("fr-FR", { month: "long" }).format(new Date(2026, value - 1, 1));
      option.textContent = monthName;
      month.append(option.cloneNode(true));
      monthlyMonth.append(option);
    }
    monthlyMonth.value = String(this.selectedBudgetMonth);
  }

  showPage(page) {
    const pages = {
      budget: ["Budget", "Planifiez les revenus, dépenses et montants mis de côté sur l’année."],
      monthly: ["Prévu vs réalisé", "Comparez le prévu et le réalisé du mois."],
      transactions: ["Opérations", "Consultez les mouvements enregistrés sur vos comptes."],
      summary: ["Synthèses", "Comparez les revenus et les dépenses par mois ou par année."],
      import: ["Import CSV", "Prévisualisez, vérifiez et confirmez l’import de vos relevés."],
      mapping: ["Rapprochement", "Rattachez les catégories réelles aux postes budgétaires."],
      "category-rules": ["Catégorisation", "Gérez vos catégories et associez-leur des motifs pour classer automatiquement les opérations."]
    };
    if (!Object.hasOwn(pages, page)) page = "budget";
    this.currentPage = page;
    this.shadowRoot.querySelector("main").dataset.currentPage = page;
    for (const element of this.shadowRoot.querySelectorAll("[data-page]")) {
      element.hidden = element.dataset.page !== page || (element.id === "preview-panel" && !this.preview);
    }

    const contentGrid = this.shadowRoot.querySelector(".content-grid");
    contentGrid.hidden = page !== "transactions" && page !== "import";
    contentGrid.classList.add("single-page");
    this.shadowRoot.querySelector("#transactions").hidden = page !== "transactions";
    this.shadowRoot.querySelector("#import").hidden = page !== "import";
    this.shadowRoot.querySelector("#preview-panel").hidden = page !== "import" || !this.preview;

    const [title, description] = pages[page];
    this.shadowRoot.querySelector(".heading h1").textContent = title;
    this.shadowRoot.querySelector(".heading .intro").textContent = description;
    for (const link of this.shadowRoot.querySelectorAll(".nav a")) {
      if (link.hash === `#${page}`) {
        link.setAttribute("aria-current", "page");
      } else {
        link.removeAttribute("aria-current");
      }
    }
    window.scrollTo(0, 0);
  }

  setTheme(theme, persist = false) {
    this.dataset.theme = theme;
    document.documentElement.dataset.theme = theme;
    const isDark = theme === "dark";
    const toggle = this.shadowRoot.querySelector(".theme-toggle");
    toggle.setAttribute("aria-label", `Activer le mode ${isDark ? "clair" : "sombre"}`);
    toggle.setAttribute("aria-pressed", String(isDark));
    toggle.querySelector(".theme-toggle-icon").textContent = isDark ? "☀" : "☾";
    toggle.querySelector(".theme-toggle-label").textContent = isDark ? "Clair" : "Sombre";
    document.querySelector('meta[name="theme-color"]').content = isDark ? "#171416" : "#f4f5f1";
    if (persist) localStorage.setItem("konta-theme", theme);
  }

  setAmbiance(ambiance, persist = false) {
    this.dataset.ambiance = ambiance;
    const isCherry = ambiance === "cherry";
    const toggle = this.shadowRoot.querySelector(".ambiance-toggle");
    toggle.setAttribute("aria-label", `${isCherry ? "Désactiver" : "Activer"} l’ambiance cerisier`);
    toggle.setAttribute("aria-pressed", String(isCherry));
    toggle.querySelector(".ambiance-toggle-label").textContent = isCherry ? "Cerisier activé" : "Cerisier";
    if (persist) localStorage.setItem("konta-ambiance", ambiance);
  }

  async loadAnnualBudget() {
    const yearInput = this.shadowRoot.querySelector("#annual-budget-year");
    const year = Number(yearInput.value);
    if (!Number.isInteger(year) || year < 1 || year > 9998) {
      this.setAnnualBudgetFeedback("Saisissez une année valide comprise entre 1 et 9998.", true);
      return;
    }

    this.setAnnualBudgetFeedback("Chargement du budget…");
    try {
      const [budgetResponse, categoriesResponse] = await Promise.all([
        fetch(`/api/annual-budgets/${year}`),
        fetch("/api/categories")
      ]);
      const [budget, categories] = await Promise.all([budgetResponse.json(), categoriesResponse.json()]);
      if (!budgetResponse.ok) throw new Error(budget.detail ?? budget.title ?? "Impossible de charger le budget.");
      if (!categoriesResponse.ok) throw new Error(categories.detail ?? categories.title ?? "Impossible de charger les catégories.");
      this.annualBudget = budget;
      this.annualBudgetCategories = categories.filter(category => category.isActive);
      this.renderAnnualBudget();
      this.setAnnualBudgetFeedback("");
      await this.loadAnnualBudgetMonth();
    } catch (exception) {
      this.setAnnualBudgetFeedback(exception.message, true);
    }
  }

  async loadAnnualBudgetMonth() {
    const feedback = this.shadowRoot.querySelector("#budget-month-feedback");
    const year = this.selectedMonthlyYear;
    const month = this.selectedBudgetMonth;
    feedback.hidden = true;
    try {
      const [response, budgetResponse] = await Promise.all([
        fetch(`/api/annual-budgets/${year}/months/${month}`),
        fetch(`/api/annual-budgets/${year}`)
      ]);
      const [result, budget] = await Promise.all([response.json(), budgetResponse.json()]);
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible de charger le suivi mensuel.");
      if (!budgetResponse.ok) throw new Error(budget.detail ?? budget.title ?? "Impossible de charger le budget du suivi mensuel.");
      if (year !== this.selectedMonthlyYear || month !== this.selectedBudgetMonth) return;
      this.annualBudgetMonth = result;
      this.monthlyBudget = budget;
      this.renderAnnualBudgetMonth();
    } catch (exception) {
      feedback.textContent = exception.message;
      feedback.hidden = false;
    }
  }

  renderAnnualBudget() {
    if (!this.annualBudget) return;
    const budget = this.annualBudget;
    const activePosts = budget.posts.filter(post => post.isActive);
    const narrowLayout = window.matchMedia("(max-width: 800px)").matches;
    const months = narrowLayout ? [this.selectedBudgetMonth] : Array.from({ length: 12 }, (_, index) => index + 1);
    const monthNames = months.map(month => new Intl.DateTimeFormat("fr-FR", { month: narrowLayout ? "long" : "short" })
      .format(new Date(budget.year, month - 1, 1)));
    const head = this.shadowRoot.querySelector("#annual-budget-head");
    head.replaceChildren();
    const headerRow = document.createElement("tr");
    headerRow.append(this.budgetCell("Poste", "th"));
    months.forEach((month, index) => {
      const cell = this.budgetCell(monthNames[index], "th");
      cell.className = "budget-month-column";
      headerRow.append(cell);
    });
    headerRow.append(this.budgetCell("Total annuel", "th"));
    headerRow.append(this.budgetCell("", "th"));
    head.append(headerRow);

    const body = this.shadowRoot.querySelector("#annual-budget-body");
    body.replaceChildren();
    for (const { type, expenseKind, label, emptyLabel } of BUDGET_GROUPS) {
      const sectionRow = document.createElement("tr");
      sectionRow.className = `budget-section-row ${type === "Income" ? "income" : type === "Savings" ? "savings" : ""}`;
      const sectionCell = this.budgetCell(label, "th");
      sectionCell.colSpan = months.length + 3;
      sectionRow.append(sectionCell);
      body.append(sectionRow);

      const groupPosts = activePosts.filter(post => post.type === type
        && (type !== "Expense" || (post.expenseKind ?? "Variable") === expenseKind));
      for (const post of groupPosts) {
        const row = this.annualBudgetPostRow(post, months);
        row.dataset.orderGroup = `${type}:${expenseKind ?? ""}`;
        row.dataset.orderType = type;
        if (expenseKind) row.dataset.orderExpenseKind = expenseKind;
        body.append(row);
      }

      if (groupPosts.length === 0) {
        const emptyRow = document.createElement("tr");
        emptyRow.className = "budget-empty-row";
        const emptyCell = this.budgetCell(emptyLabel, "td");
        emptyCell.colSpan = months.length + 3;
        emptyRow.append(emptyCell);
        body.append(emptyRow);
      }
    }

    const archivedPosts = budget.posts.filter(post => !post.isActive);
    if (archivedPosts.length > 0) {
      const sectionRow = document.createElement("tr");
      sectionRow.className = "budget-section-row archived";
      const sectionCell = this.budgetCell("Postes archivés", "th");
      sectionCell.colSpan = months.length + 3;
      sectionRow.append(sectionCell);
      body.append(sectionRow);
      for (const post of archivedPosts) body.append(this.annualBudgetPostRow(post, months));
    }

    this.renderAnnualBudgetTotals(months);
    this.shadowRoot.querySelector("#annual-budget-empty").hidden = budget.posts.length > 0;
    this.shadowRoot.querySelector(".annual-budget-table-wrap").hidden = false;
    this.updateMonthlyTitle();
  }

  updateMonthlyTitle() {
    this.shadowRoot.querySelector("#budget-monthly-title").textContent =
      `Prévu et réalisé · ${new Intl.DateTimeFormat("fr-FR", { month: "long" })
        .format(new Date(this.selectedMonthlyYear, this.selectedBudgetMonth - 1, 1))}`;
  }

  annualBudgetPostRow(post, months) {
    const inlineCopy = months.length > 1;
    const row = document.createElement("tr");
    row.dataset.postId = post.id;
    if (!post.isActive) row.className = "budget-archived-row";

    const nameCell = document.createElement("td");
    const nameControls = document.createElement("div");
    nameControls.className = "budget-post-name-controls";
    if (post.isActive) {
      const handle = this.budgetIconButton({
        label: `Déplacer ${post.name}. Utilisez les flèches haut et bas pour changer sa position`,
        title: "Glisser pour déplacer, ou utiliser les flèches haut et bas",
        dataAttribute: "movePost",
        paths: ["M8 5h8", "M8 9h8", "M8 13h8", "M8 17h8"]
      });
      handle.classList.add(
        "budget-drag-handle",
        post.type === "Income" ? "budget-drag-handle-income"
          : post.type === "Savings" ? "budget-drag-handle-savings"
            : "budget-drag-handle-expense"
      );
      handle.draggable = true;
      nameControls.append(handle);
    }
    const nameInput = document.createElement("input");
    nameInput.type = "text";
    nameInput.maxLength = 100;
    nameInput.value = post.name;
    nameInput.disabled = !post.isActive;
    nameInput.dataset.postName = "";
    nameInput.setAttribute("aria-label", `Nom du poste, ${post.name}`);
    nameControls.append(nameInput);
    nameCell.append(nameControls);
    row.append(nameCell);

    for (const month of months) {
      const cell = document.createElement("td");
      cell.className = "budget-month-column";
      const input = document.createElement("input");
      input.type = "number";
      input.min = "0";
      input.step = "0.01";
      input.inputMode = "decimal";
      input.value = post.monthlyAmounts[month - 1] ?? "";
      input.disabled = !post.isActive;
      input.dataset.monthAmount = "";
      input.dataset.month = month;
      input.setAttribute("aria-label", `${post.name}, ${new Intl.DateTimeFormat("fr-FR", { month: "long" })
        .format(new Date(this.annualBudget.year, month - 1, 1))}`);
      cell.append(input);
      if (inlineCopy && post.isActive && month < 12) {
        cell.classList.add("budget-month-editable");
        const copy = this.budgetIconButton({
          label: `Recopier le montant de ${post.name} sur les mois suivants`,
          title: "Recopier ce montant sur les mois suivants",
          dataAttribute: "copyForward",
          paths: ["M5 12h14", "M13 6l6 6-6 6"]
        });
        copy.dataset.month = month;
        copy.classList.add("budget-inline-copy");
        cell.append(copy);
      }
      row.append(cell);
    }

    const totalCell = document.createElement("td");
    totalCell.className = "amount";
    totalCell.dataset.postTotal = "";
    totalCell.textContent = this.formatCurrency(post.annualTotal);
    row.append(totalCell);

    const actionCell = document.createElement("td");
    actionCell.className = "budget-actions-cell";
    if (post.isActive && !inlineCopy) {
      actionCell.append(this.budgetIconButton({
        label: `Recopier le montant de ${post.name} sur les mois suivants`,
        title: "Recopier le montant du dernier mois modifié sur les mois suivants",
        dataAttribute: "copyForward",
        paths: ["M5 12h14", "M13 6l6 6-6 6"]
      }));
    }
    const deleteButton = this.budgetIconButton({
      label: `Supprimer le poste ${post.name}`,
      title: "Supprimer le poste",
      dataAttribute: "deletePost",
      paths: ["M4 7h16", "M10 11v6", "M14 11v6", "M6 7l1 13h10l1-13", "M9 7V4h6v3"]
    });
    deleteButton.classList.add("budget-post-delete");
    actionCell.append(deleteButton);
    row.append(actionCell);

    return row;
  }

  budgetIconButton({ label, title, dataAttribute, paths }) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "secondary-button icon-button";
    button.title = title;
    button.dataset[dataAttribute] = "";
    button.setAttribute("aria-label", label);
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("viewBox", "0 0 24 24");
    svg.setAttribute("aria-hidden", "true");
    for (const definition of paths) {
      const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
      path.setAttribute("d", definition);
      svg.append(path);
    }
    button.append(svg);
    return button;
  }

  renderAnnualBudgetTotals(months) {
    const incomeTotal = this.annualBudgetTypeTotal("Income");
    const expenseTotal = this.annualBudgetTypeTotal("Expense");
    const savingsTotal = this.annualBudgetTypeTotal("Savings");
    const availableTotal = this.annualBudget.availableTotal;
    const types = [
      ["Income", "Total revenus"],
      ["Expense", "Total dépenses"],
      ["Savings", "Total épargne prévue"],
      ["Available", "Disponible prévisionnel"]
    ];
    const foot = this.shadowRoot.querySelector("#annual-budget-foot");
    foot.replaceChildren();
    for (const [type, label] of types) {
      const row = document.createElement("tr");
      row.className = type === "Available" ? "budget-available-row" : "budget-total-row";
      row.append(this.budgetCell(label, "th"));
      for (const month of months) {
        const amount = this.monthlyBudgetTotal(type, month);
        const cell = this.budgetCell(this.formatCurrency(amount), "td");
        cell.className = "amount budget-month-column";
        if (type === "Available") {
          const balanceClass = this.budgetBalanceClass(amount);
          if (balanceClass) {
            cell.classList.add(balanceClass);
          }
        }
        row.append(cell);
      }
      const annualTotal = type === "Available"
        ? availableTotal
        : type === "Income"
          ? incomeTotal
          : type === "Expense"
            ? expenseTotal
            : savingsTotal;
      row.append(this.budgetCell(this.formatCurrency(annualTotal), "td"));
      row.lastChild.className = "amount";
      if (type === "Available") {
        const balanceClass = this.budgetBalanceClass(annualTotal);
        if (balanceClass) {
          row.lastChild.classList.add(balanceClass);
        }
      }
      row.append(this.budgetCell("", "td"));
      foot.append(row);
    }

    this.appendAnnualBudgetBalanceRow(
      foot,
      "Report du mois précédent",
      "CarryForward",
      months,
      null,
      "budget-carry-forward-row"
    );
    this.appendAnnualBudgetBalanceRow(
      foot,
      "Résultat prévisionnel du mois",
      "MonthlyNet",
      months,
      null,
      "budget-month-result-row"
    );

    const cards = this.shadowRoot.querySelector("#annual-budget-totals");
    cards.replaceChildren();
    for (const [label, amount, style] of [
      ["Revenus prévus", incomeTotal, "income"],
      ["Dépenses prévues", expenseTotal, "expense"],
      ["Épargne prévue", savingsTotal, "savings"],
      ["Disponible annuel", availableTotal, "available"]
    ]) {
      const card = document.createElement("article");
      card.className = `annual-budget-total ${style}`;
      const title = document.createElement("span");
      title.textContent = label;
      const value = document.createElement("strong");
      value.textContent = this.formatCurrency(amount);
      card.append(title, value);
      cards.append(card);
    }
  }

  appendAnnualBudgetBalanceRow(foot, label, totalType, months, annualTotal, rowClass) {
    const row = document.createElement("tr");
    row.className = rowClass;
    row.append(this.budgetCell(label, "th"));
    for (const month of months) {
      const amount = totalType === "CarryForward"
        ? this.monthlyBudgetTotal(totalType, month)
        : this.monthlyBudgetTotal("Available", month)
          + this.monthlyBudgetTotal("CarryForward", month);
      const cell = this.budgetCell(this.formatCurrency(amount), "td");
      cell.className = "amount budget-month-column";
      const balanceClass = this.budgetBalanceClass(amount);
      if (balanceClass) {
        cell.classList.add(balanceClass);
      }
      row.append(cell);
    }
    row.append(this.budgetCell(
      annualTotal === null ? "—" : this.formatCurrency(annualTotal),
      "td"
    ));
    row.lastChild.className = "amount";
    row.append(this.budgetCell("", "td"));
    foot.append(row);
  }

  budgetBalanceClass(amount) {
    return amount > 0 ? "budget-balance-positive" : amount < 0 ? "budget-balance-negative" : "";
  }

  annualBudgetTypeTotal(type) {
    return this.annualBudget.posts
      .filter(post => post.isActive && post.type === type)
      .reduce((total, post) => total + Math.round(post.annualTotal * 100), 0) / 100;
  }

  monthlyBudgetTotal(type, month) {
    if (type === "CarryForward") {
      return this.annualBudget.plannedCarryForwardAmounts[month - 1];
    }
    const totalFor = postType => this.annualBudget.posts
      .filter(post => post.isActive && post.type === postType)
      .reduce((total, post) => total + Math.round((post.monthlyAmounts[month - 1] ?? 0) * 100), 0);
    const income = totalFor("Income");
    const expense = totalFor("Expense");
    const savings = totalFor("Savings");
    const total = type === "Income" ? income
      : type === "Expense" ? expense
        : type === "Savings" ? savings
          : income - expense - savings;
    return total / 100;
  }

  monthTotalLine(label, value, modifier = "") {
    const line = document.createElement("div");
    line.className = `budget-month-line ${modifier}`.trim();
    const name = document.createElement("span");
    name.textContent = label;
    const amount = document.createElement("span");
    amount.className = "budget-month-line-amount";
    amount.textContent = value;
    line.append(name, amount);
    return line;
  }

  renderAnnualBudgetMonth() {
    if (!this.annualBudgetMonth) return;
    const summary = this.annualBudgetMonth;
    const period = new Intl.DateTimeFormat("fr-FR", { month: "long", year: "numeric" })
      .format(new Date(summary.year, summary.month - 1, 1));
    this.shadowRoot.querySelector("#budget-monthly-period").textContent = period;
    const totals = this.shadowRoot.querySelector("#budget-monthly-totals");
    totals.replaceChildren();
    for (const [label, planned, actual, style] of [
      ["Revenus", summary.plannedIncomeTotal, summary.actualIncomeTotal, "income"],
      ["Dépenses", summary.plannedExpenseTotal, summary.actualExpenseTotal, "expense"],
      ["Épargne", summary.plannedSavingsTotal, summary.actualSavingsTotal, "savings"],
      ["Disponible", summary.plannedAvailableTotal, summary.actualAvailableTotal, "available"]
    ]) {
      const card = document.createElement("article");
      card.className = `budget-month-total ${style}`;
      const title = document.createElement("strong");
      title.textContent = label;
      card.append(title, this.monthTotalLine("Prévu", this.formatCurrency(planned)));
      if (actual !== null) {
        const difference = actual - planned;
        const sign = difference > 0 ? "+" : difference < 0 ? "−" : "±";
        card.append(
          this.monthTotalLine("Réalisé", this.formatCurrency(actual)),
          this.monthTotalLine("Écart", `${sign} ${this.formatCurrency(Math.abs(difference))}`, "difference")
        );
      }
      totals.append(card);
    }

    const body = this.shadowRoot.querySelector("#budget-monthly-body");
    body.replaceChildren();
    for (const { type, expenseKind, label, emptyLabel, typeLabel } of BUDGET_GROUPS) {
      const sectionRow = document.createElement("tr");
      sectionRow.className = `budget-section-row ${type === "Income" ? "income" : type === "Savings" ? "savings" : ""}`;
      const sectionCell = this.budgetCell(label, "th");
      sectionCell.colSpan = 5;
      sectionRow.append(sectionCell);
      body.append(sectionRow);

      const posts = summary.posts.filter(post => post.type === type
        && (type !== "Expense" || (post.expenseKind ?? "Variable") === expenseKind));
      if (posts.length === 0) {
        const row = document.createElement("tr");
        const emptyCell = this.budgetCell(emptyLabel, "td");
        emptyCell.colSpan = 5;
        row.append(emptyCell);
        body.append(row);
      }

      for (const post of posts) {
        const row = document.createElement("tr");
        row.append(this.budgetCell(typeLabel, "td"), this.budgetCell(post.name, "td"));
        row.lastChild.className = "budget-month-post-name";
        row.append(this.budgetCell(
          post.plannedAmount === null ? "—" : this.formatCurrency(post.plannedAmount),
          "td"
        ));
        const actualCell = document.createElement("td");
        if (post.actualAmount === null) {
          actualCell.textContent = "À rapprocher";
          actualCell.className = "budget-unmapped";
        } else {
          actualCell.textContent = this.formatCurrency(post.actualAmount);
          if (post.transactionCount > 0) {
            actualCell.append(this.budgetTransactionDetails(post.transactions, `${post.transactionCount} opération${post.transactionCount === 1 ? "" : "s"}`));
          }
        }
        row.append(actualCell);
        const varianceCell = document.createElement("td");
        if (post.varianceAmount === null) {
          varianceCell.textContent = "—";
        } else {
          const roundedVariance = Math.round(post.varianceAmount * 100);
          const varianceBadge = document.createElement("span");
          varianceBadge.className = `budget-variance-badge ${roundedVariance > 0 ? "over" : roundedVariance < 0 ? "under" : "within"}`;
          const varianceValue = document.createElement("span");
          varianceValue.textContent = this.formatSignedCurrency(post.varianceAmount);
          varianceCell.className = "budget-variance-cell";
          varianceBadge.append(varianceValue);
          if (roundedVariance === 0) {
            const check = document.createElementNS("http://www.w3.org/2000/svg", "svg");
            check.setAttribute("viewBox", "0 0 24 24");
            check.setAttribute("role", "img");
            check.setAttribute("aria-label", "Prévu aligné sur le réalisé");
            const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
            path.setAttribute("d", "M5 12l5 5L19 7");
            check.append(path);
            varianceBadge.append(check);
          } else if (post.actualAmount >= 0) {
            varianceBadge.append(this.alignPlannedButton(post));
          }
          varianceCell.append(varianceBadge);
        }
        row.append(varianceCell);
        row.dataset.postId = post.id;
        body.append(row);
      }
    }

    body.onclick = event => {
      const button = event.target.closest("[data-align-planned]");
      if (button) this.alignPlannedWithActual(button);
    };

    const unassigned = this.shadowRoot.querySelector("#budget-unassigned");
    unassigned.replaceChildren();
    unassigned.hidden = summary.unassignedTransactionCount === 0;
    if (summary.unassignedTransactionCount > 0) {
      const heading = document.createElement("div");
      heading.className = "budget-unassigned-heading";
      const title = document.createElement("strong");
      title.textContent = `${summary.unassignedTransactionCount} opération${summary.unassignedTransactionCount === 1 ? "" : "s"} non rapprochée${summary.unassignedTransactionCount === 1 ? "" : "s"}`;
      const amount = document.createElement("span");
      amount.textContent = `Revenus ${this.formatCurrency(summary.unassignedIncomeTotal)} · Dépenses ${this.formatCurrency(summary.unassignedExpenseTotal)}`;
      heading.append(title, amount);
      unassigned.append(heading, this.budgetTransactionDetails(summary.unassignedTransactions, "Voir les opérations à rapprocher"));
      const help = document.createElement("p");
      help.textContent = "Rattachez leurs catégories au poste correspondant dans la colonne « Catégories réelles » du suivi mensuel pour les inclure dans les comparaisons.";
      unassigned.append(help);
    }
  }

  alignPlannedButton(post) {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "secondary-button align-planned-button";
    button.dataset.alignPlanned = post.id;
    button.title = "Aligne le poste budgétaire avec le réel pour ce mois-ci";
    button.setAttribute("aria-label", `Aligner le prévu du poste ${post.name} sur le réalisé (${this.formatCurrency(post.actualAmount)})`);
    const icon = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    icon.setAttribute("viewBox", "0 0 24 24");
    icon.setAttribute("aria-hidden", "true");
    for (const definition of ["M5 9h14", "M5 15h14"]) {
      const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
      path.setAttribute("d", definition);
      icon.append(path);
    }
    button.append(icon);
    return button;
  }

  async alignPlannedWithActual(button) {
    const post = this.annualBudgetMonth?.posts.find(item => item.id === button.dataset.alignPlanned);
    if (!post || post.actualAmount === null) return;
    const feedback = this.shadowRoot.querySelector("#budget-month-feedback");
    const { year, month } = this.annualBudgetMonth;
    button.disabled = true;
    feedback.hidden = true;
    try {
      const response = await fetch(`/api/annual-budgets/${year}/posts/${post.id}/months/${month}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ amount: Math.round(post.actualAmount * 100) / 100 })
      });
      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        throw new Error(problem?.detail ?? problem?.title ?? "Impossible d’aligner le prévu sur le réalisé.");
      }
      await Promise.all([this.loadAnnualBudgetMonth(), this.loadAnnualBudget()]);
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
      button.disabled = false;
    }
  }

  budgetTransactionDetails(transactions, summaryText) {
    const details = document.createElement("details");
    details.className = "budget-transaction-details";
    const summary = document.createElement("summary");
    summary.textContent = summaryText;
    details.append(summary);
    const table = document.createElement("table");
    const head = document.createElement("thead");
    const header = document.createElement("tr");
    for (const label of ["Date", "Opération", "Compte", "Type", "Montant"]) {
      header.append(this.budgetCell(label, "th"));
    }
    head.append(header);
    const tableBody = document.createElement("tbody");
    for (const transaction of transactions) tableBody.append(this.transactionRow(transaction));
    table.append(head, tableBody);
    details.append(table);
    return details;
  }

  budgetCell(text, tagName) {
    const cell = document.createElement(tagName);
    cell.textContent = text;
    return cell;
  }

  async createAnnualBudgetPost(event) {
    event.preventDefault();
    const nameInput = this.shadowRoot.querySelector("#annual-budget-post-name");
    const type = this.shadowRoot.querySelector("#annual-budget-post-type").value;
    try {
      const response = await fetch(`/api/annual-budgets/posts`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: nameInput.value.trim(),
          type,
          expenseKind: type === "Expense"
            ? this.shadowRoot.querySelector("#annual-budget-expense-kind").value
            : null
        })
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible d’ajouter le poste.");
      nameInput.value = "";
      await this.loadAnnualBudget();
      this.setAnnualBudgetFeedback(`Poste « ${result.name} » ajouté.`);
      nameInput.focus();
    } catch (exception) {
      this.setAnnualBudgetFeedback(exception.message, true);
    }
  }

  async handleAnnualBudgetChange(event) {
    const input = event.target;
    const row = input.closest("[data-post-id]");
    if (!row) return;
    const post = this.annualBudget?.posts.find(item => item.id === row.dataset.postId)
      ?? this.monthlyBudget?.posts.find(item => item.id === row.dataset.postId);
    if (!post) return;
    try {
      let response;
      if (input.matches("[data-post-name]")) {
        response = await fetch(`/api/annual-budgets/posts/${post.id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name: row.querySelector("[data-post-name]").value.trim(),
            type: post.type,
            expenseKind: post.type === "Expense" ? post.expenseKind ?? "Variable" : null
          })
        });
      } else if (input.matches("[data-month-amount]")) {
        const month = input.dataset.month;
        response = input.value === ""
          ? await fetch(`/api/annual-budgets/${this.annualBudget.year}/posts/${post.id}/months/${month}`, { method: "DELETE" })
          : await fetch(`/api/annual-budgets/${this.annualBudget.year}/posts/${post.id}/months/${month}`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ amount: Number(input.value) })
          });
      } else {
        return;
      }

      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible d’enregistrer cette modification.");
      const index = this.annualBudget.posts.findIndex(item => item.id === result.id);
      if (index < 0) throw new Error("Le poste modifié est absent du budget affiché.");
      this.annualBudget.posts[index] = result;
      row.querySelector("[data-post-total]").textContent = this.formatCurrency(result.annualTotal);
      this.renderAnnualBudgetTotals(
        window.matchMedia("(max-width: 800px)").matches
          ? [this.selectedBudgetMonth]
          : Array.from({ length: 12 }, (_, monthIndex) => monthIndex + 1)
      );
      this.setAnnualBudgetFeedback("Modification enregistrée.");
      await this.loadAnnualBudgetMonth();
    } catch (exception) {
      this.setAnnualBudgetFeedback(exception.message, true);
      await this.loadAnnualBudget();
    }
  }

  async loadMapping() {
    try {
      const year = this.annualBudget?.year ?? new Date().getFullYear();
      const [budgetResponse, orphansResponse] = await Promise.all([
        fetch(`/api/annual-budgets/${year}`),
        fetch("/api/categories/orphans")
      ]);
      if (!budgetResponse.ok || !orphansResponse.ok) throw new Error("Impossible de charger le rapprochement.");
      const [budget, orphans] = await Promise.all([budgetResponse.json(), orphansResponse.json()]);
      this.mappingPosts = budget.posts.filter(post => post.isActive);
      this.mappingOrphans = orphans;
      this.renderMapping();
    } catch (exception) {
      this.setMappingFeedback(exception.message, true);
    }
  }

  setMappingFeedback(message, isError = false) {
    const feedback = this.shadowRoot.querySelector("#mapping-feedback");
    feedback.textContent = message;
    feedback.className = isError ? "feedback error" : "feedback";
    feedback.hidden = !message;
  }

  renderMapping() {
    const badge = this.shadowRoot.querySelector("#mapping-badge");
    badge.textContent = String(this.mappingOrphans.length);
    badge.hidden = this.mappingOrphans.length === 0;

    const orphans = this.shadowRoot.querySelector("#mapping-orphans");
    orphans.replaceChildren();
    const orphansTitle = document.createElement("h3");
    orphansTitle.textContent = `À rattacher (${this.mappingOrphans.length})`;
    orphans.append(orphansTitle);
    if (this.mappingOrphans.length === 0) {
      const empty = document.createElement("p");
      empty.className = "mapping-empty";
      empty.textContent = "Toutes les catégories sont rattachées à un poste.";
      orphans.append(empty);
    }
    for (const orphan of this.mappingOrphans) {
      const row = document.createElement("div");
      row.className = "mapping-orphan";
      row.dataset.categoryId = orphan.id;
      const name = document.createElement("strong");
      name.textContent = orphan.name;
      const detail = document.createElement("span");
      detail.textContent = `${orphan.transactionCount} opération${orphan.transactionCount === 1 ? "" : "s"} · ${this.formatCurrency(orphan.totalAmount)}`;
      const select = document.createElement("select");
      select.dataset.mappingPostSelect = "";
      select.setAttribute("aria-label", `Poste pour la catégorie ${orphan.name}`);
      const placeholder = document.createElement("option");
      placeholder.value = "";
      placeholder.textContent = "Choisir un poste…";
      select.append(placeholder);
      for (const { type, expenseKind, label } of BUDGET_GROUPS) {
        const posts = this.mappingPosts
          .filter(post => post.type === type
            && (type !== "Expense" || (post.expenseKind ?? "Variable") === expenseKind))
          .sort((first, second) => first.name.localeCompare(second.name, "fr", { sensitivity: "base" }));
        if (posts.length === 0) continue;
        const optgroup = document.createElement("optgroup");
        optgroup.label = label;
        for (const post of posts) {
          const option = document.createElement("option");
          option.value = post.id;
          option.textContent = post.name;
          optgroup.append(option);
        }
        select.append(optgroup);
      }
      const button = document.createElement("button");
      button.type = "button";
      button.className = "secondary-button mapping-attach-button";
      button.dataset.attachCategory = "";
      button.title = "Rattacher au poste";
      button.setAttribute("aria-label", `Rattacher la catégorie ${orphan.name} au poste choisi`);
      const icon = document.createElementNS("http://www.w3.org/2000/svg", "svg");
      icon.setAttribute("viewBox", "0 0 24 24");
      icon.setAttribute("aria-hidden", "true");
      for (const definition of ["M10 13a5 5 0 0 0 7 0l3-3a5 5 0 0 0-7-7l-1 1", "M14 11a5 5 0 0 0-7 0l-3 3a5 5 0 0 0 7 7l1-1"]) {
        const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", definition);
        icon.append(path);
      }
      button.append(icon);
      const heading = document.createElement("div");
      heading.className = "mapping-orphan-heading";
      heading.append(name, detail);
      const controls = document.createElement("div");
      controls.className = "mapping-orphan-controls";
      controls.append(select, button);
      row.append(heading, controls);
      orphans.append(row);
    }

    const posts = this.shadowRoot.querySelector("#mapping-posts");
    posts.replaceChildren();
    for (const { type, label } of [
      { type: "Income", label: "Revenus" },
      { type: "Expense", label: "Dépenses" },
      { type: "Savings", label: "Épargne" }
    ]) {
      const group = this.mappingPosts.filter(post => post.type === type);
      if (group.length === 0) continue;
      const title = document.createElement("h3");
      title.textContent = label;
      posts.append(title);
      for (const post of group) {
        const row = document.createElement("div");
        row.className = "mapping-post";
        const name = document.createElement("strong");
        name.textContent = post.name;
        const pills = document.createElement("div");
        pills.className = "mapping-pills";
        if (post.categories.length === 0) {
          const none = document.createElement("span");
          none.className = "mapping-empty";
          none.textContent = "Aucune catégorie rattachée";
          pills.append(none);
        }
        for (const category of post.categories) {
          const pill = document.createElement("span");
          pill.className = "mapping-pill";
          pill.append(document.createTextNode(category.name));
          const remove = document.createElement("button");
          remove.type = "button";
          remove.dataset.detachCategory = category.id;
          remove.dataset.postId = post.id;
          remove.textContent = "×";
          remove.setAttribute("aria-label", `Détacher ${category.name} du poste ${post.name}`);
          pill.append(remove);
          pills.append(pill);
        }
        row.append(name, pills);
        posts.append(row);
      }
    }
  }

  async handleMappingClick(event) {
    const attach = event.target.closest("[data-attach-category]");
    const detach = event.target.closest("[data-detach-category]");
    if (attach) {
      const row = attach.closest("[data-category-id]");
      const postId = row.querySelector("[data-mapping-post-select]").value;
      if (!postId) {
        this.setMappingFeedback("Choisissez un poste avant de rattacher la catégorie.", true);
        return;
      }
      const post = this.mappingPosts.find(item => item.id === postId);
      await this.saveMapping(post, [...post.categories.map(category => category.id), row.dataset.categoryId]);
    } else if (detach) {
      const post = this.mappingPosts.find(item => item.id === detach.dataset.postId);
      await this.saveMapping(post, post.categories.map(category => category.id).filter(id => id !== detach.dataset.detachCategory));
    }
  }

  async saveMapping(post, categoryIds) {
    try {
      const response = await fetch(`/api/annual-budgets/posts/${post.id}/categories`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ categoryIds })
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible d’enregistrer le rapprochement.");
      this.setMappingFeedback("Rapprochement enregistré.");
    } catch (exception) {
      this.setMappingFeedback(exception.message, true);
    }
    await this.loadMapping();
    if (this.annualBudget) await this.loadAnnualBudgetMonth();
  }

  async handleAnnualBudgetAction(event) {
    const copyButton = event.target.closest("[data-copy-forward]");
    if (copyButton) {
      await this.copyBudgetAmountForward(copyButton);
      return;
    }

    const button = event.target.closest("[data-delete-post]");
    if (!button) return;
    const row = button.closest("[data-post-id]");
    const post = this.annualBudget?.posts.find(item => item.id === row?.dataset.postId);
    if (!post || !window.confirm(`Supprimer « ${post.name} » ? Attention : le poste sera supprimé pour toutes les années, avec tous ses montants budgétés et ses rapprochements de catégories. Cette action est irréversible.`)) return;
    button.disabled = true;
    try {
      const response = await fetch(`/api/annual-budgets/posts/${post.id}`, { method: "DELETE" });
      if (!response.ok) {
        const result = await response.json();
        throw new Error(result.detail ?? result.title ?? "Impossible de supprimer le poste.");
      }
      await this.loadAnnualBudget();
      this.setAnnualBudgetFeedback(`Poste « ${post.name} » supprimé.`);
    } catch (exception) {
      this.setAnnualBudgetFeedback(exception.message, true);
      button.disabled = false;
    }
  }

  handleBudgetPostDragStart(event) {
    const handle = event.target.closest("[data-move-post]");
    const row = handle?.closest("[data-post-id]");
    if (!row || !event.dataTransfer) return;
    this.draggedBudgetPostId = row.dataset.postId;
    row.classList.add("budget-post-dragging");
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", row.dataset.postId);
  }

  handleBudgetPostDragOver(event) {
    const target = event.target.closest("tr[data-post-id]");
    const dragged = this.draggedBudgetPostId
      ? this.shadowRoot.querySelector(`[data-post-id="${this.draggedBudgetPostId}"]`)
      : null;
    if (!dragged || !target || target === dragged || target.dataset.orderGroup !== dragged.dataset.orderGroup) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    this.shadowRoot.querySelectorAll(".budget-post-drop-target").forEach(row => row.classList.remove("budget-post-drop-target"));
    target.classList.add("budget-post-drop-target");
  }

  handleBudgetPostDragLeave(event) {
    const row = event.target.closest("tr[data-post-id]");
    if (row && !row.contains(event.relatedTarget)) row.classList.remove("budget-post-drop-target");
  }

  async handleBudgetPostDrop(event) {
    const target = event.target.closest("tr[data-post-id]");
    const sourceId = this.draggedBudgetPostId;
    const source = sourceId
      ? this.shadowRoot.querySelector(`[data-post-id="${sourceId}"]`)
      : null;
    if (!source || !target || source === target || source.dataset.orderGroup !== target.dataset.orderGroup) return;
    event.preventDefault();
    const groupRows = [...this.shadowRoot.querySelectorAll(`tr[data-order-group="${source.dataset.orderGroup}"]`)];
    const postIds = groupRows.map(row => row.dataset.postId);
    const sourceIndex = postIds.indexOf(sourceId);
    let targetIndex = postIds.indexOf(target.dataset.postId);
    const bounds = target.getBoundingClientRect();
    if (event.clientY > bounds.top + bounds.height / 2) targetIndex++;
    postIds.splice(sourceIndex, 1);
    if (sourceIndex < targetIndex) targetIndex--;
    postIds.splice(targetIndex, 0, sourceId);
    await this.persistBudgetPostOrder(source, postIds);
  }

  handleBudgetPostMoveKeydown(event) {
    const handle = event.target.closest("[data-move-post]");
    const row = handle?.closest("[data-post-id]");
    if (!row || !["ArrowUp", "ArrowDown"].includes(event.key)) return;
    const groupRows = [...this.shadowRoot.querySelectorAll(`tr[data-order-group="${row.dataset.orderGroup}"]`)];
    const postIds = groupRows.map(item => item.dataset.postId);
    const index = postIds.indexOf(row.dataset.postId);
    const targetIndex = index + (event.key === "ArrowUp" ? -1 : 1);
    if (targetIndex < 0 || targetIndex >= postIds.length) return;
    event.preventDefault();
    [postIds[index], postIds[targetIndex]] = [postIds[targetIndex], postIds[index]];
    this.persistBudgetPostOrder(row, postIds, row.dataset.postId);
  }

  clearBudgetPostDragState() {
    this.draggedBudgetPostId = null;
    this.shadowRoot.querySelectorAll(".budget-post-dragging, .budget-post-drop-target").forEach(row => {
      row.classList.remove("budget-post-dragging", "budget-post-drop-target");
    });
  }

  async persistBudgetPostOrder(row, postIds, focusPostId = null) {
    const type = row.dataset.orderType;
    const expenseKind = row.dataset.orderExpenseKind || null;
    try {
      const response = await fetch(`/api/annual-budgets/posts/order`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ type, expenseKind, postIds })
      });
      if (!response.ok) {
        const result = await response.json();
        throw new Error(result.detail ?? result.title ?? "Impossible d’enregistrer l’ordre des postes.");
      }
      this.clearBudgetPostDragState();
      await this.loadAnnualBudget();
      if (focusPostId) {
        this.shadowRoot.querySelector(`[data-post-id="${focusPostId}"] [data-move-post]`)?.focus();
      }
      this.setAnnualBudgetFeedback("Ordre des postes enregistré.");
    } catch (exception) {
      this.clearBudgetPostDragState();
      this.setAnnualBudgetFeedback(exception.message, true);
      await this.loadAnnualBudget();
    }
  }

  async copyBudgetAmountForward(button) {
    const row = button.closest("[data-post-id]");
    const post = this.annualBudget?.posts.find(item => item.id === row?.dataset.postId);
    if (!post) return;
    const inputs = [...row.querySelectorAll("[data-month-amount]")];
    const sourceMonth = button.dataset.month ?? row.dataset.sourceMonth;
    const source = inputs.find(input => input.dataset.month === sourceMonth && input.value !== "")
      ?? inputs.find(input => input.value !== "");
    if (!source) {
      this.setAnnualBudgetFeedback("Saisissez d’abord un montant à recopier.", true);
      return;
    }
    if (source.dataset.month === "12") {
      this.setAnnualBudgetFeedback("Décembre est le dernier mois : il n’y a rien à recopier.", true);
      return;
    }

    button.disabled = true;
    try {
      const base = `/api/annual-budgets/${this.annualBudget.year}/posts/${post.id}/months/${source.dataset.month}`;
      const saved = await fetch(base, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ amount: Number(source.value) })
      });
      if (!saved.ok) throw new Error((await saved.json()).detail ?? "Impossible d’enregistrer le montant à recopier.");
      const response = await fetch(`${base}/copy-forward`, { method: "POST" });
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible de recopier le montant.");
      await this.loadAnnualBudget();
      const monthName = new Intl.DateTimeFormat("fr-FR", { month: "long" })
        .format(new Date(this.annualBudget.year, Number(source.dataset.month) - 1, 1));
      this.setAnnualBudgetFeedback(`Montant de ${monthName} recopié sur les mois suivants pour « ${post.name} ».`);
    } catch (exception) {
      this.setAnnualBudgetFeedback(exception.message, true);
      button.disabled = false;
    }
  }

  setAnnualBudgetFeedback(message, isError = false) {
    const feedback = this.shadowRoot.querySelector("#annual-budget-feedback");
    feedback.textContent = message;
    feedback.className = `feedback${isError ? " error" : ""}`;
    feedback.hidden = !message;
  }

  async loadTransactions() {
    const empty = this.shadowRoot.querySelector("#transactions-empty");
    const body = this.shadowRoot.querySelector("#transactions-body");
    const error = this.shadowRoot.querySelector("#transactions-error");
    try {
      const response = await fetch("/api/transactions");
      if (!response.ok) throw new Error("Impossible de charger les opérations.");
      const transactions = await response.json();
      this.transactions = transactions;
      this.shadowRoot.querySelector("#transaction-count").textContent = `${transactions.length} opération${transactions.length === 1 ? "" : "s"}`;
      empty.hidden = transactions.length > 0;
      body.replaceChildren();
      for (const transaction of transactions) body.append(this.transactionRow(transaction));
      this.renderBalanceForm();
    } catch (exception) {
      error.textContent = exception.message;
      error.hidden = false;
      this.shadowRoot.querySelector("#transaction-count").textContent = "indisponible";
    }
  }

  async loadPhase2Data() {
    const feedback = this.shadowRoot.querySelector("#rules-feedback");
    try {
      const [categoriesResponse, accountsResponse, rulesResponse] = await Promise.all([
        fetch("/api/categories"),
        fetch("/api/accounts"),
        fetch("/api/category-rules")
      ]);
      if (!categoriesResponse.ok || !accountsResponse.ok || !rulesResponse.ok) {
        throw new Error("Impossible de charger les catégories, comptes ou associations.");
      }
      [this.categories, this.accounts, this.categoryRules] = await Promise.all([
        categoriesResponse.json(),
        accountsResponse.json(),
        rulesResponse.json()
      ]);
      this.annualBudgetCategories = this.categories.filter(category => category.isActive);
      this.renderPhase2Data();
      await this.loadMapping();
      await this.loadSummary();
      if (this.annualBudget) {
        this.renderAnnualBudget();
        await this.loadAnnualBudgetMonth();
      }
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    }
  }

  renderBalanceForm() {
    const select = this.shadowRoot.querySelector("#balance-account");
    const selected = select.value;
    select.replaceChildren(this.option("", "Choisir un compte"));
    for (const account of this.accounts.filter(item => item.isActive)) {
      select.append(this.option(account.id, account.name));
    }
    const hasSelection = [...select.options].some(option => option.value === selected);
    select.value = hasSelection ? selected : "";
    this.renderBalanceTransactions(!hasSelection);
    if (hasSelection) this.loadBalance();
  }

  latestAccountTransaction(accountId) {
    return this.transactions
      .filter(item => item.accountId === accountId)
      .reduce((latest, item) => (!latest || item.date >= latest.date ? item : latest), null);
  }

  renderBalanceTransactions(selectLatest = false) {
    const accountId = this.shadowRoot.querySelector("#balance-account").value;
    const transactionSelect = this.shadowRoot.querySelector("#balance-transaction");
    const selected = transactionSelect.value;
    const transactions = this.transactions
      .filter(item => item.accountId === accountId)
      .sort((left, right) => right.date.localeCompare(left.date));
    transactionSelect.replaceChildren(this.option("", "Choisir une opération"));
    for (const transaction of transactions) {
      transactionSelect.append(this.option(
        transaction.id,
        `${this.formatDate(transaction.date)} · ${transaction.label} · ${this.formatSignedCurrency(transaction.type === "Income" ? transaction.amount : -transaction.amount)}`
      ));
    }
    const latest = this.latestAccountTransaction(accountId);
    if (!selectLatest && [...transactionSelect.options].some(option => option.value === selected)) {
      transactionSelect.value = selected;
    } else if (latest) {
      transactionSelect.value = latest.id;
    }
  }

  showBalanceFeedback(message, isError) {
    const feedback = this.shadowRoot.querySelector("#balance-feedback");
    feedback.className = `feedback ${isError ? "error" : "success"}`;
    feedback.textContent = message;
    feedback.hidden = false;
  }

  async setBalanceReference(event) {
    event.preventDefault();
    const accountId = this.shadowRoot.querySelector("#balance-account").value;
    try {
      const response = await fetch(`/api/accounts/${accountId}/balances/reference`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          transactionId: this.shadowRoot.querySelector("#balance-transaction").value,
          balanceAfterTransaction: Number(this.shadowRoot.querySelector("#reference-balance").value)
        })
      });
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible d’enregistrer le solde.");
      this.showBalanceFeedback("Solde enregistré.", false);
      this.renderBalanceResult(result);
    } catch (exception) {
      this.showBalanceFeedback(exception.message, true);
    }
  }

  async loadBalance() {
    const accountId = this.shadowRoot.querySelector("#balance-account").value;
    if (!accountId) {
      this.shadowRoot.querySelector("#balance-result").textContent = "";
      return;
    }
    try {
      const response = await fetch(`/api/accounts/${accountId}/balances`);
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible de charger le solde.");
      this.renderBalanceResult(result);
    } catch (exception) {
      this.showBalanceFeedback(exception.message, true);
    }
  }

  renderBalanceResult(result) {
    const output = this.shadowRoot.querySelector("#balance-result");
    output.textContent = result.actualBalance === null
      ? "Aucun solde défini pour ce compte."
      : `Solde constaté : ${this.formatCurrency(result.actualBalance)} (référence du ${this.formatDate(result.referenceDate)} : ${this.formatCurrency(result.referenceBalance)}).`;
  }

  renderPhase2Data() {
    this.renderCategoryOptions(this.shadowRoot.querySelector("#rule-category"));
    this.renderCategoryOptions(this.shadowRoot.querySelector("#summary-category"), "Toutes les catégories");
    const accountSelect = this.shadowRoot.querySelector("#summary-account");
    accountSelect.replaceChildren(this.option("", "Tous les comptes"));
    for (const account of this.accounts.filter(item => item.isActive)) {
      accountSelect.append(this.option(account.id, account.name));
    }
    this.renderCategoryRules();
    this.renderCategories();
    this.renderBalanceForm();
  }

  renderCategoryOptions(select, placeholder = "Choisir une catégorie") {
    const selected = select.value;
    select.replaceChildren(this.option("", placeholder));
    for (const category of this.categories.filter(item => item.isActive)) {
      select.append(this.option(category.id, category.name));
    }
    if ([...select.options].some(option => option.value === selected)) select.value = selected;
  }

  option(value, label) {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = label;
    return option;
  }

  renderCategoryRules() {
    this.shadowRoot.querySelector("#rules-count").textContent =
      `${this.categoryRules.length} association${this.categoryRules.length === 1 ? "" : "s"}`;
  }

  renderCategories() {
    const list = this.shadowRoot.querySelector("#category-list");
    list.replaceChildren();
    if (this.categories.length === 0) return;
    const heading = document.createElement("h3");
    heading.textContent = "Catégories et motifs";
    list.append(heading);
    for (const category of this.categories.filter(item => item.isActive)) {
      const group = document.createElement("article");
      group.className = "category-group";
      const row = document.createElement("div");
      row.className = "category-item";
      const name = document.createElement("strong");
      name.textContent = category.name;
      const actions = document.createElement("div");
      actions.className = "rule-actions";
      const edit = this.budgetIconButton({
        label: `Renommer la catégorie ${category.name}`,
        title: "Renommer",
        dataAttribute: "editCategory",
        paths: ["M12 20h9", "M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4z"]
      });
      edit.dataset.editCategory = category.id;
      const remove = this.budgetIconButton({
        label: `Désactiver la catégorie ${category.name}`,
        title: "Désactiver",
        dataAttribute: "deleteCategory",
        paths: ["M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z", "M5.6 5.6l12.8 12.8"]
      });
      remove.dataset.deleteCategory = category.id;
      remove.classList.add("danger-button");
      const add = this.budgetIconButton({
        label: `Ajouter un motif à la catégorie ${category.name}`,
        title: "Ajouter un motif",
        dataAttribute: "addRuleToCategory",
        paths: ["M12 5v14", "M5 12h14"]
      });
      add.dataset.addRuleToCategory = category.id;
      actions.append(edit, remove, add);
      row.append(name, actions);
      group.append(row);
      const rules = this.categoryRules.filter(rule => rule.categoryId === category.id);
      if (rules.length > 0) {
        const patterns = document.createElement("ul");
        patterns.className = "pattern-list";
        for (const rule of rules) patterns.append(this.createRuleItem(rule));
        group.append(patterns);
      }
      list.append(group);
    }
  }

  createRuleItem(rule) {
    const item = document.createElement("li");
    item.className = "pattern-item";
    const text = document.createElement("span");
    text.className = "pattern-text";
    const typeLabel = OPERATION_TYPES[rule.operationType] ?? "Tous les types";
    const icon = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    icon.setAttribute("viewBox", "0 0 24 24");
    icon.setAttribute("class", "pattern-type-icon");
    icon.setAttribute("role", "img");
    icon.setAttribute("aria-label", typeLabel);
    const title = document.createElementNS("http://www.w3.org/2000/svg", "title");
    title.textContent = typeLabel;
    icon.append(title);
    for (const definition of OPERATION_TYPE_ICONS[rule.operationType] ?? OPERATION_TYPE_ICONS.Any) {
      const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
      path.setAttribute("d", definition);
      icon.append(path);
    }
    text.append(icon, rule.pattern);
    const actions = document.createElement("div");
    actions.className = "rule-actions";
    const edit = this.budgetIconButton({
      label: `Modifier le motif ${rule.pattern}`,
      title: "Modifier",
      dataAttribute: "editRule",
      paths: ["M4 20h4l10-10-4-4L4 16z", "M13 7l4 4"]
    });
    edit.dataset.editRule = rule.id;
    const remove = this.budgetIconButton({
      label: `Supprimer le motif ${rule.pattern}`,
      title: "Supprimer",
      dataAttribute: "deleteRule",
      paths: ["M4 7h16", "M10 11v6", "M14 11v6", "M6 7l1 13h10l1-13", "M9 7V4h6v3"]
    });
    remove.dataset.deleteRule = rule.id;
    remove.classList.add("danger-button");
    actions.append(edit, remove);
    item.append(text, actions);
    return item;
  }

  // Le nom reste exigé et unique côté API ; il est dérivé de la catégorie, du type et du motif.
  buildRuleName(categoryId, pattern, operationType) {
    const category = this.categories.find(item => item.id === categoryId)?.name ?? "";
    const type = OPERATION_TYPES[operationType];
    return [category, type, pattern].filter(Boolean).join(" · ").slice(0, 100);
  }

  async saveCategoryRule(event) {
    event.preventDefault();
    const feedback = this.shadowRoot.querySelector("#rules-feedback");
    const button = this.shadowRoot.querySelector("#rule-submit");
    button.disabled = true;
    try {
      const pattern = this.shadowRoot.querySelector("#rule-pattern").value.trim();
      const operationType = this.shadowRoot.querySelector("#rule-operation-type").value || null;
      const categoryId = this.shadowRoot.querySelector("#rule-category").value;
      const rule = {
        name: this.buildRuleName(categoryId, pattern, operationType),
        pattern,
        operationType,
        categoryId
      };
      const isEditing = Boolean(this.editingRuleId);
      const response = await fetch(
        isEditing ? `/api/category-rules/${this.editingRuleId}` : "/api/category-rules",
        {
          method: isEditing ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(rule)
        }
      );
      const result = response.status === 204 ? null : await response.json();
      if (!response.ok) throw new Error(result?.detail ?? result?.title ?? "Impossible d’enregistrer l’association.");
      feedback.className = "feedback success";
      feedback.textContent = isEditing ? "Association mise à jour." : "Association créée.";
      feedback.hidden = false;
      this.resetRuleForm();
      await this.loadPhase2Data();
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    } finally {
      button.disabled = false;
    }
  }

  resetRuleForm() {
    this.editingRuleId = null;
    this.shadowRoot.querySelector("#category-rule-form").reset();
    this.shadowRoot.querySelector("#rule-submit").textContent = "Associer";
    this.shadowRoot.querySelector("#rule-cancel").hidden = true;
  }

  async handleRuleAction(event) {
    const addButton = event.target.closest("[data-add-rule-to-category]");
    if (addButton) {
      this.resetRuleForm();
      this.shadowRoot.querySelector("#rule-category").value = addButton.dataset.addRuleToCategory;
      const pattern = this.shadowRoot.querySelector("#rule-pattern");
      this.shadowRoot.querySelector("#category-rule-form").scrollIntoView({ block: "center" });
      pattern.focus();
      return;
    }

    const editButton = event.target.closest("[data-edit-rule]");
    if (editButton) {
      const rule = this.categoryRules.find(item => item.id === editButton.dataset.editRule);
      if (!rule) return;
      this.editingRuleId = rule.id;
      this.shadowRoot.querySelector("#rule-pattern").value = rule.pattern;
      this.shadowRoot.querySelector("#rule-operation-type").value = rule.operationType ?? "";
      this.shadowRoot.querySelector("#rule-category").value = rule.categoryId;
      this.shadowRoot.querySelector("#rule-submit").textContent = "Enregistrer";
      this.shadowRoot.querySelector("#rule-cancel").hidden = false;
      this.shadowRoot.querySelector("#rule-pattern").focus();
      this.shadowRoot.querySelector("#category-rule-form").scrollIntoView({ block: "center" });
      return;
    }

    const deleteButton = event.target.closest("[data-delete-rule]");
    if (!deleteButton) return;
    const feedback = this.shadowRoot.querySelector("#rules-feedback");
    deleteButton.disabled = true;
    try {
      const response = await fetch(`/api/category-rules/${deleteButton.dataset.deleteRule}`, { method: "DELETE" });
      if (!response.ok) {
        const problem = await response.json();
        throw new Error(problem.detail ?? problem.title ?? "Impossible de supprimer l’association.");
      }
      feedback.className = "feedback success";
      feedback.textContent = "Association supprimée.";
      feedback.hidden = false;
      await this.loadPhase2Data();
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
      deleteButton.disabled = false;
    }
  }

  async createCategory(event) {
    event.preventDefault();
    const feedback = this.shadowRoot.querySelector("#rules-feedback");
    try {
      const isEditing = Boolean(this.editingCategoryId);
      const response = await fetch(
        isEditing ? `/api/categories/${this.editingCategoryId}` : "/api/categories",
        {
        method: isEditing ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: this.shadowRoot.querySelector("#new-category-name").value.trim() })
        }
      );
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible de créer la catégorie.");
      this.resetCategoryForm();
      feedback.className = "feedback success";
      feedback.textContent = isEditing
        ? `Catégorie renommée « ${result.name} ».`
        : `Catégorie « ${result.name} » créée.`;
      feedback.hidden = false;
      await this.loadPhase2Data();
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    }
  }

  resetCategoryForm() {
    this.editingCategoryId = null;
    this.shadowRoot.querySelector("#category-form").reset();
    this.shadowRoot.querySelector("#category-submit").textContent = "Ajouter une catégorie";
    this.shadowRoot.querySelector("#category-cancel").hidden = true;
  }

  async handleCategoryAction(event) {
    const editButton = event.target.closest("[data-edit-category]");
    if (editButton) {
      const category = this.categories.find(item => item.id === editButton.dataset.editCategory);
      if (!category) return;
      this.editingCategoryId = category.id;
      this.shadowRoot.querySelector("#new-category-name").value = category.name;
      this.shadowRoot.querySelector("#category-submit").textContent = "Enregistrer";
      this.shadowRoot.querySelector("#category-cancel").hidden = false;
      this.shadowRoot.querySelector("#new-category-name").focus();
      return;
    }

    const deleteButton = event.target.closest("[data-delete-category]");
    if (!deleteButton) return;
    const feedback = this.shadowRoot.querySelector("#rules-feedback");
    deleteButton.disabled = true;
    try {
      const response = await fetch(`/api/categories/${deleteButton.dataset.deleteCategory}`, { method: "DELETE" });
      if (!response.ok) {
        const problem = await response.json();
        throw new Error(problem.detail ?? problem.title ?? "Impossible de supprimer la catégorie.");
      }
      feedback.className = "feedback success";
      feedback.textContent = "Catégorie supprimée.";
      feedback.hidden = false;
      await this.loadPhase2Data();
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
      deleteButton.disabled = false;
    }
  }

  async loadSummary() {
    const error = this.shadowRoot.querySelector("#summary-error");
    const results = this.shadowRoot.querySelector("#summary-results");
    const year = this.shadowRoot.querySelector("#summary-year").value;
    const month = this.shadowRoot.querySelector("#summary-month").value;
    const accountId = this.shadowRoot.querySelector("#summary-account").value;
    const categoryId = this.shadowRoot.querySelector("#summary-category").value;
    const query = new URLSearchParams({ year });
    if (month) query.set("month", month);
    if (accountId) query.set("accountId", accountId);
    if (categoryId) query.set("categoryId", categoryId);
    error.hidden = true;
    try {
      const response = await fetch(`/api/transactions/summary?${query}`);
      const summary = await response.json();
      if (!response.ok) throw new Error(summary.detail ?? summary.title ?? "Impossible de charger la synthèse.");
      this.summary = summary;
      this.renderSummary(summary);
      results.hidden = false;
    } catch (exception) {
      error.textContent = exception.message;
      error.hidden = false;
      results.hidden = true;
    }
  }

  renderSummary(summary) {
    this.shadowRoot.querySelector("#summary-count").textContent =
      `${summary.transactionCount} opération${summary.transactionCount === 1 ? "" : "s"}`;
    const period = summary.month
      ? `${new Intl.DateTimeFormat("fr-FR", { month: "long" }).format(new Date(summary.year, summary.month - 1, 1))} ${summary.year}`
      : `Année ${summary.year}`;
    this.shadowRoot.querySelector("#summary-period").textContent = period;
    const totals = this.shadowRoot.querySelector("#summary-totals");
    totals.replaceChildren();
    for (const [label, amount, descriptor, style] of [
      ["Revenus", summary.incomeTotal, "Total des entrées", "income"],
      ["Dépenses", summary.expenseTotal, "Total des sorties", "expense"],
      ["Solde net", summary.netTotal, "Revenus moins dépenses", "net"]
    ]) {
      const card = document.createElement("div");
      const sentiment = style === "net" && amount < 0 ? "negative" : "positive";
      card.className = `summary-total ${style} ${sentiment}`;
      const title = document.createElement("span");
      title.textContent = label;
      const value = document.createElement("strong");
      value.textContent = this.formatCurrency(amount);
      const note = document.createElement("small");
      note.textContent = descriptor;
      card.append(title, value, note);
      totals.append(card);
    }

    const body = this.shadowRoot.querySelector("#summary-categories");
    body.replaceChildren();
    if (summary.categories.length === 0) {
      const row = document.createElement("tr");
      const emptyCell = document.createElement("td");
      emptyCell.colSpan = 5;
      emptyCell.className = "summary-empty";
      emptyCell.textContent = "Aucune opération pour cette période et ces filtres.";
      row.append(emptyCell);
      body.append(row);
    }
    const sort = this.summarySort ?? null;
    for (const header of this.shadowRoot.querySelectorAll(".summary-table th[data-sort-key]")) {
      const direction = sort?.key === header.dataset.sortKey ? sort.direction : null;
      header.querySelector(".sort-caret").textContent = { asc: "▲", desc: "▼" }[direction] ?? "⇅";
      header.setAttribute("aria-sort", { asc: "ascending", desc: "descending" }[direction] ?? "none");
    }
    const compare = {
      name: (first, second) => first.categoryName.localeCompare(second.categoryName, "fr", { sensitivity: "base" }),
      net: (first, second) => first.netTotal - second.netTotal
    };
    const categories = sort
      ? [...summary.categories].sort((first, second) => compare[sort.key](first, second) * (sort.direction === "asc" ? 1 : -1))
      : summary.categories;
    let hasOpenedCategory = false;
    categories.forEach((category, index) => {
      const row = document.createElement("tr");
      row.append(
        this.cell(category.categoryName, "summary-category-name"),
        this.cell(this.formatCurrency(category.incomeTotal), "amount"),
        this.cell(this.formatCurrency(category.expenseTotal), "amount"),
        this.cell(this.formatCurrency(category.netTotal), "amount")
      );
      const actionCell = document.createElement("td");
      const detailsButton = document.createElement("button");
      detailsButton.type = "button";
      detailsButton.className = "text-button summary-accordion-button";
      detailsButton.dataset.summaryCategory = index;
      const isExpanded = Boolean(this.hasOpenedSummaryCategory
        && (category.categoryId ?? null) === this.openedSummaryCategoryId);
      detailsButton.setAttribute("aria-expanded", String(isExpanded));
      if (isExpanded) detailsButton.setAttribute("aria-controls", `summary-category-details-${index}`);
      detailsButton.textContent = "Opérations";
      const caret = document.createElement("span");
      caret.className = "summary-accordion-caret";
      caret.setAttribute("aria-hidden", "true");
      caret.textContent = "⌄";
      detailsButton.append(caret);
      actionCell.append(detailsButton);
      row.append(actionCell);
      body.append(row);
      if (isExpanded) {
        body.append(this.renderSummaryDetails(category, index));
        hasOpenedCategory = true;
      }
    });
    if (this.hasOpenedSummaryCategory && !hasOpenedCategory) {
      this.hasOpenedSummaryCategory = false;
      this.openedSummaryCategoryId = undefined;
    }
    body.onclick = event => {
      const button = event.target.closest("[data-summary-category]");
      if (!button) return;
      const category = categories[Number(button.dataset.summaryCategory)];
      const categoryId = category.categoryId ?? null;
      if (this.hasOpenedSummaryCategory && this.openedSummaryCategoryId === categoryId) {
        this.hasOpenedSummaryCategory = false;
        this.openedSummaryCategoryId = undefined;
      } else {
        this.hasOpenedSummaryCategory = true;
        this.openedSummaryCategoryId = categoryId;
      }
      this.renderSummary(this.summary);
    };
  }

  renderSummaryDetails(category, index) {
    const row = document.createElement("tr");
    row.className = "summary-operations-row";
    const cell = document.createElement("td");
    cell.colSpan = 5;
    const details = document.createElement("div");
    details.className = "summary-operations";
    details.id = `summary-category-details-${index}`;
    details.setAttribute("role", "region");
    details.setAttribute("aria-label", `Opérations de la catégorie ${category.categoryName}`);
    const title = document.createElement("h3");
    title.textContent = `${category.categoryName} · ${category.transactions.length} opération${category.transactions.length === 1 ? "" : "s"}`;
    const table = document.createElement("table");
    table.className = "summary-operations-table";
    const head = document.createElement("thead");
    const headerRow = document.createElement("tr");
    for (const label of ["Date", "Opération", "Compte", "Type", "Montant", "Catégorie"]) {
      const th = document.createElement("th");
      th.textContent = label;
      headerRow.append(th);
    }
    head.append(headerRow);
    const body = document.createElement("tbody");
    for (const transaction of category.transactions) {
      body.append(this.transactionRow(transaction, this.transactionCategoryCell(transaction)));
    }
    table.append(head, body);
    details.append(title, table);
    cell.append(details);
    row.append(cell);
    return row;
  }

  async loadImportSettings() {
    const feedback = this.shadowRoot.querySelector("#import-source-feedback");
    try {
      const response = await fetch("/api/import-settings");
      const settings = await response.json();
      if (!response.ok) throw new Error(settings.detail ?? settings.title ?? "Impossible de charger les paramètres d’import.");
      this.shadowRoot.querySelector("#import-bank-identifier").value = settings.bankIdentifier;
      this.shadowRoot.querySelector("#import-source-url").value = settings.bankUrl;
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    }
  }

  async saveImportSettings(event) {
    event.preventDefault();
    const form = this.shadowRoot.querySelector("#import-source-form");
    if (!form.reportValidity()) return;

    const button = this.shadowRoot.querySelector("#import-source-save");
    const feedback = this.shadowRoot.querySelector("#import-source-feedback");
    button.disabled = true;
    feedback.hidden = true;
    try {
      const response = await fetch("/api/import-settings", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          bankIdentifier: this.shadowRoot.querySelector("#import-bank-identifier").value,
          bankUrl: this.shadowRoot.querySelector("#import-source-url").value
        })
      });
      const settings = await response.json();
      if (!response.ok) throw new Error(settings.detail ?? settings.title ?? "Impossible d’enregistrer les paramètres d’import.");

      this.shadowRoot.querySelector("#import-bank-identifier").value = settings.bankIdentifier;
      this.shadowRoot.querySelector("#import-source-url").value = settings.bankUrl;
      feedback.className = "feedback success";
      feedback.textContent = "Paramètres enregistrés.";
      feedback.hidden = false;
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    } finally {
      button.disabled = false;
    }
  }

  async previewFile(event) {
    event.preventDefault();
    if (!this.selectedFile) return;

    await this.requestPreview(this.selectedFile);
  }

  async requestPreview(file) {
    if (this.phase2DataPromise) await this.phase2DataPromise;
    const button = this.shadowRoot.querySelector("#preview-button");
    const feedback = this.shadowRoot.querySelector("#import-feedback");
    button.disabled = true;
    button.textContent = "Analyse en cours…";
    feedback.hidden = true;
    this.shadowRoot.querySelector("#preview-panel").hidden = true;

    try {
      const formData = new FormData();
      formData.append("file", file);
      const accountMappings = this.getAccountMappings();
      if (Object.keys(accountMappings).length > 0) {
        formData.append("accountMappings", JSON.stringify(accountMappings));
      }
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

    this.updatePreviewVisibility();
    const duplicateCount = preview.transactions.filter(transaction => transaction.isDuplicate).length;
    this.shadowRoot.querySelector("#preview-summary").textContent = `${preview.errors.length} erreur${preview.errors.length === 1 ? "" : "s"} · ${duplicateCount} doublon${duplicateCount === 1 ? "" : "s"} détecté${duplicateCount === 1 ? "" : "s"}. Vous pouvez exclure une ligne ou corriger sa catégorie et son montant avant l'import.`;
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

  updatePreviewVisibility() {
    const hideDuplicates = this.shadowRoot.querySelector("#hide-duplicates").checked;
    const hideCategorized = this.shadowRoot.querySelector("#hide-categorized").checked;
    const rows = [...this.shadowRoot.querySelectorAll("#preview-body tr")];
    let visibleCount = 0;
    for (const row of rows) {
      const categorySelect = row.querySelector("[data-category]");
      const newCategoryInput = row.querySelector("[data-new-category]");
      const categoryName = newCategoryInput.hidden ? categorySelect.value : newCategoryInput.value.trim();
      const categorySummary = row.querySelector("[data-category-summary]");
      categorySummary.textContent = categorySelect.selectedOptions[0]?.textContent ?? "Non catégorisé";
      categorySummary.setAttribute("aria-label", `Catégorie de l'opération, ligne ${row.dataset.rowNumber} : ${categorySummary.textContent}`);
      const searchLink = row.querySelector("[data-beneficiary-search]");
      if (searchLink) searchLink.hidden = Boolean(categoryName);
      row.hidden = (hideDuplicates && row.classList.contains("duplicate-row"))
        || (hideCategorized && Boolean(categoryName) && !row.contains(this.shadowRoot.activeElement));
      if (!row.hidden) visibleCount++;
    }

    const total = rows.length;
    const totalLabel = `${total} ligne${total === 1 ? "" : "s"} valide${total === 1 ? "" : "s"}`;
    const visibleLabel = `${visibleCount} ligne${visibleCount === 1 ? "" : "s"} affichée${visibleCount === 1 ? "" : "s"}`;
    this.shadowRoot.querySelector("#preview-count").textContent = hideDuplicates || hideCategorized
      ? `${visibleLabel} sur ${totalLabel}`
      : totalLabel;
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
        this.accountMappings[row.dataset.accountNumber] = result.id;
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
    const invalidControl = [...this.shadowRoot.querySelectorAll("#preview-body input, #preview-body select")]
      .find(control => control.dataset.include === undefined && !control.checkValidity());
    if (invalidControl) {
      invalidControl.reportValidity();
      return;
    }
    button.disabled = true;
    button.textContent = "Enregistrement…";
    try {
      const formData = new FormData();
      formData.append("file", this.selectedFile);
      const accountMappings = this.getAccountMappings();
      if (Object.keys(accountMappings).length > 0) {
        formData.append("accountMappings", JSON.stringify(accountMappings));
      }
      formData.append("review", JSON.stringify(this.getImportReview()));
      const response = await fetch("/api/imports/commit", { method: "POST", body: formData });
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "L'import a échoué.");
      const commitMessage = `${result.importedCount} opération${result.importedCount === 1 ? "" : "s"} ajoutée${result.importedCount === 1 ? "" : "s"}, ${result.excludedCount} exclue${result.excludedCount === 1 ? "" : "s"}, ${result.duplicateCount} doublon${result.duplicateCount === 1 ? "" : "s"} ignoré${result.duplicateCount === 1 ? "" : "s"}, ${result.errors.length} erreur${result.errors.length === 1 ? "" : "s"}.${result.balanceApplied ? " Solde du compte mis à jour d’après le relevé." : ""}`;
      feedback.className = "feedback success";
      feedback.textContent = commitMessage;
      feedback.hidden = false;
      await this.loadTransactions();
      this.phase2DataPromise = this.loadPhase2Data();
      await this.phase2DataPromise;
      await this.requestPreview(this.selectedFile);
      if (!feedback.classList.contains("error")) {
        feedback.className = "feedback success";
        feedback.textContent = commitMessage;
        feedback.hidden = false;
      }
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
      button.disabled = false;
    } finally {
      button.textContent = "Enregistrer les opérations";
    }
  }

  getAccountMappings() {
    return Object.fromEntries(Object.entries(this.accountMappings).filter(([, value]) => Boolean(value)));
  }

  getImportReview() {
    return [...this.shadowRoot.querySelectorAll("#preview-body tr")].map(row => {
      const categorySelect = row.querySelector("[data-category]");
      const newCategoryInput = row.querySelector("[data-new-category]");
      const transferMatch = row.querySelector("[data-transfer-match]")?.value;
      const amount = this.parseReviewAmount(row.querySelector("[data-amount]").value);
      const categoryName = newCategoryInput.hidden ? categorySelect.value : newCategoryInput.value.trim();
      return {
        rowNumber: Number(row.dataset.rowNumber),
        include: row.querySelector("[data-include]").checked,
        transactionDate: row.dataset.transactionDate,
        label: row.dataset.label,
        amount: Math.abs(amount),
        type: amount < 0 ? "Expense" : "Income",
        categoryName,
        createCategoryRule: row.querySelector("[data-remember-rule]").checked && Boolean(categoryName),
        rulePattern: row.querySelector("[data-rule-pattern]")?.value.trim() || null,
        confirmTransferMatch: Boolean(transferMatch),
        transferTransactionId: transferMatch ? JSON.parse(transferMatch).transactionId : null,
        transferRowNumber: transferMatch ? JSON.parse(transferMatch).rowNumber : null
      };
    });
  }

  transactionLabelCell(transaction) {
    const cell = document.createElement("td");
    const label = document.createElement("span");
    label.textContent = transaction.details || transaction.label;
    if (transaction.details) label.title = transaction.label;
    cell.append(label);
    return cell;
  }

  transactionRow(transaction, categoryCell = null) {
    const row = document.createElement("tr");
    row.append(
      this.cell(this.formatDate(transaction.date), "date"),
      this.transactionLabelCell(transaction),
      this.cell(transaction.accountName ?? transaction.accountLabel ?? "—"),
      this.typeCell(transaction.type, transaction.isTransfer),
      this.cell(this.formatAmount(transaction.amount, transaction.type), "amount")
    );
    if (categoryCell) row.append(categoryCell);
    return row;
  }

  transactionCategoryCell(transaction) {
    const cell = document.createElement("td");
    const select = document.createElement("select");
    select.className = "transaction-category-select";
    select.setAttribute("aria-label", `Catégorie de l'opération ${transaction.label}`);
    select.append(this.option("", "Non catégorisé"));
    select.options[0].disabled = true;
    for (const category of this.categories.filter(item => item.isActive)) {
      select.append(this.option(category.id, category.name));
    }
    select.value = transaction.categoryId ?? "";
    select.addEventListener("change", () => this.changeTransactionCategory(transaction, select));
    cell.append(select);
    return cell;
  }

  async changeTransactionCategory(transaction, select) {
    const error = this.shadowRoot.querySelector("#summary-error");
    const previous = transaction.categoryId ?? "";
    select.disabled = true;
    error.hidden = true;
    try {
      const response = await fetch(`/api/transactions/${transaction.id}/category`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ categoryId: select.value })
      });
      if (!response.ok) {
        const problem = await response.json().catch(() => null);
        throw new Error(problem?.detail ?? problem?.title ?? "Impossible de modifier la catégorie.");
      }
      await this.loadSummary();
    } catch (exception) {
      select.value = previous;
      select.disabled = false;
      error.textContent = exception.message;
      error.hidden = false;
    }
  }

  previewRow(transaction) {
    const row = document.createElement("tr");
    row.dataset.rowNumber = transaction.rowNumber;
    row.dataset.transactionDate = transaction.transactionDate;
    row.dataset.label = transaction.label;
    row.dataset.operation = transaction.operation ? `${transaction.operation.type}|${transaction.operation.key}` : "";
    if (transaction.isDuplicate) row.className = "duplicate-row";
    const accountName = transaction.accountName ?? `À associer · ${transaction.accountLabel}`;
    const amountInput = document.createElement("input");
    amountInput.type = "text";
    amountInput.inputMode = "decimal";
    amountInput.value = this.formatReviewAmount(transaction.amount, transaction.type);
    amountInput.required = true;
    amountInput.dataset.amount = "";
    amountInput.setAttribute("aria-label", `Montant en euros, ligne ${transaction.rowNumber}`);
    this.updateReviewAmount(amountInput);
    const includeInput = document.createElement("input");
    includeInput.type = "checkbox";
    includeInput.checked = true;
    includeInput.dataset.include = "";
    includeInput.setAttribute("aria-label", `Inclure l'opération, ligne ${transaction.rowNumber}`);
    const includeControl = document.createElement("label");
    includeControl.className = "review-toggle";
    includeControl.append(includeInput);
    if (transaction.isDuplicate) {
      const duplicateNote = document.createElement("span");
      duplicateNote.className = "duplicate-note";
      duplicateNote.textContent = "Doublon";
      includeControl.append(duplicateNote);
    }
    const categorySelect = document.createElement("select");
    categorySelect.dataset.category = "";
    categorySelect.setAttribute("aria-label", `Catégorie de l'opération, ligne ${transaction.rowNumber}`);
    categorySelect.append(this.option("", "Non catégorisé"));
    for (const categoryOption of this.categories.filter(item => item.isActive)) {
      categorySelect.append(this.option(categoryOption.name, categoryOption.name));
    }
    const addCategoryOption = this.option("__add_category__", "＋ Créer une catégorie…");
    addCategoryOption.dataset.addCategory = "";
    categorySelect.append(addCategoryOption);
    const currentCategory = transaction.categoryName ?? "";
    if (currentCategory && ![...categorySelect.options].some(option => option.value === currentCategory)) {
      categorySelect.append(this.option(currentCategory, currentCategory));
    }
    categorySelect.value = currentCategory;
    categorySelect.size = 6;
    const categoryPicker = document.createElement("details");
    categoryPicker.className = "category-picker";
    const categorySummary = document.createElement("summary");
    categorySummary.dataset.categorySummary = "";
    categorySummary.textContent = categorySelect.selectedOptions[0]?.textContent ?? "Non catégorisé";
    categorySummary.setAttribute("aria-label", `Catégorie de l'opération, ligne ${transaction.rowNumber} : ${categorySummary.textContent}`);
    const categorySearch = document.createElement("input");
    categorySearch.type = "search";
    categorySearch.placeholder = "Rechercher une catégorie";
    categorySearch.setAttribute("aria-label", `Rechercher une catégorie, ligne ${transaction.rowNumber}`);
    const noCategoryResults = document.createElement("p");
    noCategoryResults.className = "category-search-empty";
    noCategoryResults.textContent = "Aucune catégorie correspondante.";
    noCategoryResults.hidden = true;
    categoryPicker.append(categorySummary, categorySearch, categorySelect, noCategoryResults);
    categoryPicker.addEventListener("toggle", () => {
      if (!categoryPicker.open) return;
      for (const other of this.shadowRoot.querySelectorAll(".category-picker[open]")) {
        if (other !== categoryPicker) other.open = false;
      }
      categorySearch.value = "";
      this.filterPreviewCategories(categoryPicker);
      categorySearch.focus();
    });
    categorySearch.addEventListener("input", () => this.filterPreviewCategories(categoryPicker));
    categoryPicker.addEventListener("keydown", event => {
      if (event.key === "Escape" && categoryPicker.open) {
        event.preventDefault();
        categoryPicker.open = false;
        categorySummary.focus();
      } else if (event.key === "ArrowDown" && event.target === categorySearch) {
        event.preventDefault();
        categorySelect.focus();
      }
    });
    categorySelect.addEventListener("change", () => {
      categoryPicker.open = false;
      categorySummary.focus();
    });
    const newCategoryInput = document.createElement("input");
    newCategoryInput.type = "text";
    newCategoryInput.maxLength = 100;
    newCategoryInput.placeholder = "Nom de la nouvelle catégorie";
    newCategoryInput.required = false;
    newCategoryInput.hidden = true;
    newCategoryInput.dataset.newCategory = "";
    newCategoryInput.setAttribute("aria-label", `Nouvelle catégorie de l'opération, ligne ${transaction.rowNumber}`);
    const rememberRule = document.createElement("input");
    rememberRule.type = "checkbox";
    rememberRule.dataset.rememberRule = "";
    const rememberLabel = document.createElement("label");
    rememberLabel.className = "remember-rule";
    rememberLabel.append(rememberRule, transaction.operation
      ? "Mémoriser l’association"
      : "Aucune clé reconnue");
    rememberRule.disabled = !transaction.operation;
    const categoryContent = document.createElement("div");
    categoryContent.className = "preview-category";
    if (transaction.operation) {
      const operationHeading = document.createElement("div");
      operationHeading.className = "operation-heading";
      const operation = document.createElement("small");
      operation.className = "operation-key";
      operation.textContent = `${OPERATION_TYPES[transaction.operation.type] ?? transaction.operation.type} · ${transaction.operation.key}`;
      operationHeading.append(operation);
      if (["Card", "TransferReceived", "TransferSent", "DirectDebit"].includes(transaction.operation.type)
          && transaction.operation.key.trim()) {
        const searchLink = document.createElement("a");
        const searchUrl = new URL("https://www.google.com/search");
        searchUrl.searchParams.set("q", transaction.operation.key.trim());
        searchLink.href = searchUrl.href;
        searchLink.target = "_blank";
        searchLink.rel = "noopener noreferrer";
        searchLink.className = "beneficiary-search";
        searchLink.dataset.beneficiarySearch = "";
        searchLink.setAttribute("aria-label", "Recherche le bénéficiaire sur internet");
        searchLink.hidden = Boolean(currentCategory);
        const icon = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        icon.setAttribute("viewBox", "0 0 24 24");
        icon.setAttribute("aria-hidden", "true");
        const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
        path.setAttribute("d", "M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14zm5 12 4 4");
        icon.append(path);
        const tooltip = document.createElement("span");
        tooltip.className = "beneficiary-search-tooltip";
        tooltip.textContent = "Recherche le bénéficiaire sur internet";
        tooltip.setAttribute("aria-hidden", "true");
        searchLink.append(icon, tooltip);
        operationHeading.append(searchLink);
      }
      categoryContent.append(operationHeading);
    }
    categoryContent.append(categoryPicker, newCategoryInput, rememberLabel);
    if (transaction.operation) {
      const patternInput = document.createElement("input");
      patternInput.type = "text";
      patternInput.maxLength = 200;
      patternInput.value = transaction.operation.key;
      patternInput.hidden = true;
      patternInput.dataset.rulePattern = "";
      patternInput.title = "Motif de l’association : raccourcissez-le pour couvrir plusieurs variantes (ex. AUCHAN)";
      patternInput.setAttribute("aria-label", `Motif de l’association, ligne ${transaction.rowNumber}`);
      rememberRule.addEventListener("change", () => { patternInput.hidden = !rememberRule.checked; });
      categoryContent.append(patternInput);
    }
    if (transaction.categorySuggestion) {
      const suggestion = document.createElement("small");
      suggestion.className = "category-suggestion";
      suggestion.textContent = `Suggestion · ${transaction.categorySuggestion.ruleName} (${transaction.categorySuggestion.pattern}${transaction.categorySuggestion.operationType ? ` · ${OPERATION_TYPES[transaction.categorySuggestion.operationType]}` : ""})`;
      categoryContent.append(suggestion);
    }
    const transferCandidates = (transaction.transferMatchCandidates ?? []).filter(candidate =>
      candidate.rowNumber === null
      || !this.preview.transactions.find(item => item.rowNumber === candidate.rowNumber)?.isDuplicate
    );
    if (!transaction.isDuplicate && transferCandidates.length > 0) {
      const transferSelect = document.createElement("select");
      transferSelect.dataset.transferMatch = "";
      transferSelect.setAttribute("aria-label", `Rapprochement de transfert, ligne ${transaction.rowNumber}`);
      transferSelect.append(this.option("", "Transfert éventuel ?"));
      for (const candidate of transferCandidates) {
        const otherRow = candidate.rowNumber ? ` · ligne ${candidate.rowNumber}` : "";
        const option = this.option(JSON.stringify({
          transactionId: candidate.transactionId,
          rowNumber: candidate.rowNumber
        }), `${candidate.accountName} · ${candidate.label} · ${this.formatDate(candidate.date)}${otherRow}`);
        transferSelect.append(option);
      }
      const matchNote = document.createElement("small");
      matchNote.className = "transfer-suggestion";
      matchNote.textContent = transferCandidates[0].isAmbiguous
        ? "Plusieurs mouvements opposés de même montant et proches en date : confirmez le bon transfert."
        : "Montant identique, compte différent et date proche. Confirmez uniquement s’il s’agit du même transfert.";
      categoryContent.append(transferSelect, matchNote);
    }
    row.append(
      this.cell(this.formatDate(transaction.transactionDate), "date"),
      this.detailsCell(transaction.fullLabel || transaction.label),
      this.cell(`${accountName} · ${transaction.accountNumber}`),
      this.controlCell(categoryContent, "category"),
      this.controlCell(amountInput, "review-amount"),
      this.controlCell(includeControl, "review-include")
    );
    return row;
  }

  refreshPendingCategoryOptions() {
    const known = new Set(this.categories.map(category => category.name.toLocaleLowerCase("fr-FR")));
    const pending = new Map();
    for (const input of this.shadowRoot.querySelectorAll("#preview-body [data-new-category]")) {
      const name = input.hidden ? "" : input.value.trim();
      const key = name.toLocaleLowerCase("fr-FR");
      if (name && !known.has(key) && !pending.has(key)) pending.set(key, name);
    }

    for (const select of this.shadowRoot.querySelectorAll("#preview-body [data-category]")) {
      const selected = select.value;
      for (const option of [...select.options]) {
        if (option.hasAttribute("data-pending-category") && option.value !== selected) option.remove();
      }
      const addOption = select.querySelector("[data-add-category]");
      for (const name of pending.values()) {
        if ([...select.options].some(option => option.value === name)) continue;
        const option = this.option(name, name);
        option.dataset.pendingCategory = "";
        select.insertBefore(option, addOption);
      }
      select.value = selected;
      const picker = select.closest(".category-picker");
      if (picker.open) this.filterPreviewCategories(picker);
    }
  }

  filterPreviewCategories(picker) {
    const normalize = value => value.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("fr-FR");
    const query = normalize(picker.querySelector('input[type="search"]').value.trim());
    const select = picker.querySelector("[data-category]");
    let matches = 0;
    for (const option of select.options) {
      if (option.hasAttribute("data-add-category")) {
        option.hidden = false;
        continue;
      }
      option.hidden = Boolean(query) && (!option.value || !normalize(option.textContent).includes(query));
      if (option.value && !option.hidden) matches++;
    }
    picker.querySelector(".category-search-empty").hidden = !query || matches > 0;
  }

  updateReviewCategory(select) {
    const newCategoryInput = select.closest("tr").querySelector("[data-new-category]");
    const isCreating = select.selectedOptions[0]?.hasAttribute("data-add-category") ?? false;
    newCategoryInput.hidden = !isCreating;
    newCategoryInput.required = isCreating;
    if (isCreating) newCategoryInput.focus();
    else newCategoryInput.value = "";
  }

  syncMatchingReviewRows(sourceRow) {
    if (this.syncingPreviewRows) return;
    const operation = sourceRow.dataset.operation;
    if (!operation) return;

    const normalizedLabel = operation.toLocaleLowerCase("fr-FR");
    const categorySelect = sourceRow.querySelector("[data-category]");
    const newCategoryInput = sourceRow.querySelector("[data-new-category]");
    const categoryName = newCategoryInput.hidden ? categorySelect.value : newCategoryInput.value.trim();
    const rememberRule = sourceRow.querySelector("[data-remember-rule]").checked;
    const sourcePattern = sourceRow.querySelector("[data-rule-pattern]");
    this.syncingPreviewRows = true;
    try {
      for (const row of this.shadowRoot.querySelectorAll("#preview-body tr")) {
        if (row === sourceRow
            || row.dataset.operation.toLocaleLowerCase("fr-FR") !== normalizedLabel) {
          continue;
        }

        const targetCategory = row.querySelector("[data-category]");
        const targetNewCategory = row.querySelector("[data-new-category]");
        targetNewCategory.hidden = true;
        targetNewCategory.required = false;
        targetNewCategory.value = "";
        if (categoryName && ![...targetCategory.options].some(option => option.value === categoryName)) {
          const option = this.option(categoryName, categoryName);
          option.dataset.pendingCategory = "";
          targetCategory.insertBefore(option, targetCategory.querySelector("[data-add-category]"));
        }
        targetCategory.value = categoryName;
        row.querySelector("[data-remember-rule]").checked = rememberRule;
        const targetPattern = row.querySelector("[data-rule-pattern]");
        if (targetPattern) {
          targetPattern.hidden = !rememberRule;
          if (sourcePattern) targetPattern.value = sourcePattern.value;
        }
      }
    } finally {
      this.syncingPreviewRows = false;
    }
  }

  formatReviewAmount(amount, type) {
    const sign = type === 0 || type === "Expense" ? "-" : "+";
    return `${sign}${new Intl.NumberFormat("fr-FR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)}`;
  }

  parseReviewAmount(value) {
    const text = value.trim();
    if (!/^[+-]?(?:(?:\d{1,3}(?:[ \u00a0\u202f]\d{3})+|\d+)(?:[.,]\d{1,2})?|[.,]\d{1,2})$/.test(text)) {
      return NaN;
    }
    return Number(text.replace(/[ \u00a0\u202f]/g, "").replace(",", "."));
  }

  updateReviewAmount(input) {
    const amount = this.parseReviewAmount(input.value);
    const isValid = Number.isFinite(amount) && amount !== 0;
    input.setCustomValidity(isValid || input.value === "" ? "" : "Saisissez un montant signé non nul, avec au plus deux décimales.");
    input.classList.toggle("expense-amount", isValid && amount < 0);
    input.classList.toggle("income-amount", isValid && amount > 0);
  }

  controlCell(control, className = "") {
    const cell = document.createElement("td");
    cell.className = className;
    cell.append(control);
    return cell;
  }

  detailsCell(detail) {
    const cell = document.createElement("td");
    cell.className = "preview-details";
    for (const line of this.splitDetailLines(detail)) {
      const element = document.createElement("div");
      element.textContent = line;
      cell.append(element);
    }
    return cell;
  }

  splitDetailLines(detail) {
    const text = detail.replace(/\s+/g, " ").trim();
    const card = /^(CARTE X\d+ \d{2}\/\d{2}) (.*?) (\d{10,}[A-Z0-9]*)$/.exec(text);
    if (card) return [card[1], card[2], card[3]];
    return text.split(/ (?=[A-ZÉ]{2,}:)/).map(line => line.trim()).filter(Boolean);
  }

  cell(text, className = "") {
    const cell = document.createElement("td");
    cell.className = className;
    cell.textContent = text;
    return cell;
  }

  typeCell(type, isTransfer = false) {
    const cell = document.createElement("td");
    const badge = document.createElement("span");
    const isExpense = type === 0 || type === "Expense";
    badge.className = `type ${isTransfer ? "transfer" : isExpense ? "expense" : "income"}`;
    badge.textContent = isTransfer ? "Transfert" : isExpense ? "Dépense" : "Revenu";
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

  formatCurrency(amount) {
    return new Intl.NumberFormat("fr-FR", { style: "currency", currency: "EUR" }).format(amount);
  }

  formatSignedCurrency(amount) {
    return amount > 0 ? `+${this.formatCurrency(amount)}` : this.formatCurrency(amount);
  }

  formatPercent(value) {
    return new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 1 }).format(value);
  }
}

customElements.define("konta-app", KontaApp);