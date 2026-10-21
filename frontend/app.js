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
    this.planningBudgets = [];
    this.forecasts = [];
    this.savingsGoals = [];
    this.editingSavingsGoalId = null;
  }

  connectedCallback() {
    this.renderShell();
    this.setTheme(localStorage.getItem("konta-theme") === "dark" ? "dark" : "light");
    this.handlePageNavigation = () => {
      this.showPage(window.location.hash.slice(1) || "transactions");
    };
    window.addEventListener("hashchange", this.handlePageNavigation);
    window.addEventListener("popstate", this.handlePageNavigation);
    this.showPage(window.location.hash.slice(1) || "transactions");
    this.loadTransactions();
    this.phase2DataPromise = this.loadPhase2Data();
    this.loadPlanningData();
  }

  disconnectedCallback() {
    window.removeEventListener("hashchange", this.handlePageNavigation);
    window.removeEventListener("popstate", this.handlePageNavigation);
  }

  renderShell() {
    this.shadowRoot.innerHTML = `
      <link rel="stylesheet" href="/styles.css">
      <div class="shell">
        <header class="topbar">
          <a class="brand" href="/" aria-label="Konta, accueil"><span class="brand-mark">K</span>Konta</a>
          <nav class="nav" aria-label="Navigation principale">
            <a href="#transactions" aria-current="page">Opérations</a>
            <a href="#summary">Synthèses</a>
            <a href="#category-rules">Règles</a>
            <a href="#planning">Planification</a>
            <a href="#import">Import CSV</a>
            <button class="theme-toggle" type="button" aria-label="Activer le mode sombre" aria-pressed="false">
              <span class="theme-toggle-icon" aria-hidden="true">☾</span><span class="theme-toggle-label">Sombre</span>
            </button>
          </nav>
        </header>
        <main>
          <div class="heading">
            <div><div class="eyebrow">Espace finances · 01</div><h1>Opérations</h1><p class="intro">Consultez les mouvements et prévisualisez un relevé avant traitement.</p></div>
          </div>
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
                <thead><tr><th>Catégorie</th><th>Revenus</th><th>Dépenses</th><th>Solde net</th><th></th></tr></thead>
                <tbody id="summary-categories"></tbody>
              </table></div>
              <section class="summary-details" id="summary-details" aria-live="polite" hidden></section>
            </div>
          </section>
          <section class="panel planning-panel" id="planning" data-page="planning" aria-labelledby="planning-title">
            <div class="panel-head"><h2 id="planning-title">Budgets, échéances et soldes</h2></div>
            <div class="planning-grid">
              <section class="planning-section" aria-labelledby="budgets-title">
                <h3 id="budgets-title">Budgets mensuels</h3>
                <form class="planning-form" id="budget-form">
                  <label>Catégorie<select id="budget-category" required></select></label>
                  <label>Budget mensuel (€)<input id="budget-amount" type="number" min="0.01" step="0.01" required></label>
                  <button type="submit">Ajouter</button>
                </form>
                <div id="budget-list" class="planning-list"></div>
              </section>
              <section class="planning-section" aria-labelledby="forecasts-title">
                <h3 id="forecasts-title">Échéances prévisionnelles</h3>
                <form class="planning-form forecast-form" id="forecast-form">
                  <label>Nom<input id="forecast-name" maxlength="100" required></label>
                  <label>Motif du libellé<input id="forecast-pattern" maxlength="200" required></label>
                  <label>Compte<select id="forecast-account" required></select></label>
                  <label>Montant prévu (€)<input id="forecast-amount" type="number" min="0.01" step="0.01" required></label>
                  <label>Type<select id="forecast-type"><option value="Expense">Dépense</option><option value="Income">Revenu</option></select></label>
                  <label>Jour du mois<input id="forecast-day" type="number" min="1" max="31" value="1" required></label>
                  <label>Récurrence<select id="forecast-frequency"><option value="Monthly">Mensuelle</option><option value="Annual">Annuelle</option></select></label>
                  <label>Début<input id="forecast-start" type="date" required></label>
                  <label>Catégorie<select id="forecast-category"></select></label>
                  <button type="submit">Ajouter l’échéance</button>
                </form>
                <div id="forecast-list" class="planning-list"></div>
                <h4>Échéances du mois</h4>
                <div id="occurrence-list" class="planning-list"></div>
              </section>
              <section class="planning-section" aria-labelledby="balances-title">
                <h3 id="balances-title">Soldes des comptes</h3>
                <form class="planning-form balance-form" id="balance-form">
                  <label>Compte<select id="balance-account" required></select></label>
                  <label>Opération de référence<select id="balance-transaction" required></select></label>
                  <label>Solde juste après l’opération (€)<input id="reference-balance" type="number" step="0.01" required></label>
                  <button type="submit">Définir le solde de référence</button>
                </form>
                <p id="balance-result" class="balance-result" role="status"></p>
              </section>
              <section class="planning-section" aria-labelledby="savings-goals-title">
                <h3 id="savings-goals-title">Objectifs d’épargne</h3>
                <form class="planning-form savings-goal-form" id="savings-goal-form">
                  <label>Nom<input id="savings-goal-name" maxlength="100" required></label>
                  <label>Compte d’épargne<select id="savings-goal-account" required></select></label>
                  <label>Montant cible (€)<input id="savings-goal-target" type="number" min="0.01" step="0.01" required></label>
                  <label>Contribution mensuelle prévue (€)<input id="savings-goal-contribution" type="number" min="0.01" step="0.01" required></label>
                  <button id="savings-goal-submit" type="submit">Créer l’objectif</button>
                  <button id="savings-goal-cancel" class="secondary-button" type="button" hidden>Annuler</button>
                </form>
                <div id="savings-goal-list" class="planning-list"></div>
              </section>
            </div>
            <p class="feedback" id="planning-feedback" role="status" aria-live="polite" hidden></p>
          </section>
          <div class="content-grid">
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
            <div class="table-wrap"><table class="preview-table">
              <thead><tr><th>Date</th><th>Libellé</th><th>Compte Konta</th><th>Catégorie / suggestion</th><th style="text-align:right">Montant (€)</th><th>Inclure</th></tr></thead>
              <tbody id="preview-body"></tbody>
            </table></div>
            <div class="account-setup" id="account-setup" hidden></div>
            <div class="preview-actions"><button id="commit-button" type="button" disabled>Enregistrer les opérations</button></div>
            <div class="errors" id="preview-errors" hidden></div>
          </section>
          <section class="panel rules-panel" id="category-rules" data-page="category-rules" aria-labelledby="rules-title">
            <div class="panel-head"><h2 id="rules-title">Règles de catégorisation</h2><span class="count" id="rules-count"></span></div>
            <p class="rules-intro">Une règle associe automatiquement une catégorie aux libellés correspondants. En cas de plusieurs correspondances, le motif le plus long est prioritaire.</p>
            <form class="rule-form" id="category-rule-form">
              <label>Nom de la règle<input id="rule-name" name="name" maxlength="100" required></label>
              <label>Motif à rechercher<input id="rule-pattern" name="pattern" maxlength="200" required></label>
              <label>Catégorie<select id="rule-category" name="categoryId" required></select></label>
              <label>Sous-catégorie (facultatif)<input id="rule-subcategory" name="subcategoryName" maxlength="200"></label>
              <div class="rule-form-actions"><button id="rule-submit" type="submit">Créer la règle</button><button id="rule-cancel" type="button" class="secondary-button" hidden>Annuler</button></div>
            </form>
            <form class="category-form" id="category-form">
              <label>Nouvelle catégorie<input id="new-category-name" maxlength="100" required></label>
              <button id="category-submit" type="submit" class="secondary-button">Ajouter une catégorie</button>
              <button id="category-cancel" type="button" class="secondary-button" hidden>Annuler</button>
            </form>
            <div class="category-list" id="category-list" aria-label="Catégories actives"></div>
            <p class="feedback" id="rules-feedback" role="status" aria-live="polite" hidden></p>
            <div class="rule-list" id="rule-list"></div>
          </section>
        </main>
      </div>`;

    this.shadowRoot.querySelector("#csv-file").addEventListener("change", event => {
      this.selectedFile = event.target.files[0] ?? null;
      this.shadowRoot.querySelector("#file-name").textContent = event.target.files[0]?.name ?? "Aucun fichier choisi";
    });
    this.shadowRoot.querySelector(".nav").addEventListener("click", event => {
      if (event.target.closest(".theme-toggle")) {
        this.setTheme(this.dataset.theme === "dark" ? "light" : "dark", true);
        return;
      }
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
    this.shadowRoot.querySelector("#import-form").addEventListener("submit", event => this.previewFile(event));
    this.shadowRoot.querySelector("#preview-panel").addEventListener("click", event => this.handlePreviewAction(event));
    this.shadowRoot.querySelector("#summary-form").addEventListener("submit", event => {
      event.preventDefault();
      this.loadSummary();
    });
    this.shadowRoot.querySelector("#category-rule-form").addEventListener("submit", event => this.saveCategoryRule(event));
    this.shadowRoot.querySelector("#rule-cancel").addEventListener("click", () => this.resetRuleForm());
    this.shadowRoot.querySelector("#category-form").addEventListener("submit", event => this.createCategory(event));
    this.shadowRoot.querySelector("#category-cancel").addEventListener("click", () => this.resetCategoryForm());
    this.shadowRoot.querySelector("#rule-list").addEventListener("click", event => this.handleRuleAction(event));
    this.shadowRoot.querySelector("#category-list").addEventListener("click", event => this.handleCategoryAction(event));
    this.shadowRoot.querySelector("#budget-form").addEventListener("submit", event => this.createBudget(event));
    this.shadowRoot.querySelector("#budget-list").addEventListener("click", event => this.handleBudgetAction(event));
    this.shadowRoot.querySelector("#forecast-form").addEventListener("submit", event => this.createForecast(event));
    this.shadowRoot.querySelector("#forecast-list").addEventListener("click", event => this.handleForecastAction(event));
    this.shadowRoot.querySelector("#balance-form").addEventListener("submit", event => this.setBalanceReference(event));
    this.shadowRoot.querySelector("#savings-goal-form").addEventListener("submit", event => this.saveSavingsGoal(event));
    this.shadowRoot.querySelector("#savings-goal-list").addEventListener("click", event => this.handleSavingsGoalAction(event));
    this.shadowRoot.querySelector("#savings-goal-cancel").addEventListener("click", () => this.resetSavingsGoalForm());
    this.shadowRoot.querySelector("#balance-account").addEventListener("change", () => {
      this.renderBalanceTransactions();
      this.loadBalance();
    });
    this.shadowRoot.querySelector("#balance-transaction").addEventListener("change", () => this.loadBalance());
    this.shadowRoot.querySelector("#summary-year").addEventListener("change", () => this.loadPlanningData());
    this.shadowRoot.querySelector("#summary-month").addEventListener("change", () => this.loadPlanningData());
    this.shadowRoot.querySelector("#preview-body").addEventListener("change", event => {
      if (event.target.matches("[data-category]")) {
        this.updateReviewCategory(event.target);
        const row = event.target.closest("tr");
        if (row.querySelector("[data-subcategory]").value.trim()) {
          row.dataset.createSubcategoryRule = "true";
        }
        this.syncMatchingReviewRows(row);
      }
      if (event.target.matches("[data-forecast-match]")) {
        const row = event.target.closest("tr");
        row.dataset.confirmForecast = event.target.value;
      }
    });
    this.shadowRoot.querySelector("#preview-body").addEventListener("input", event => {
      if (event.target.matches("[data-amount]")) this.updateReviewAmount(event.target);
      if (event.target.matches("[data-subcategory], [data-new-category]")) {
        const row = event.target.closest("tr");
        if (event.target.matches("[data-subcategory]")) {
          row.dataset.createSubcategoryRule = event.target.value.trim() ? "true" : "false";
        }
        this.syncMatchingReviewRows(row);
      }
    });

    const year = this.shadowRoot.querySelector("#summary-year");
    year.value = new Date().getFullYear();
    this.shadowRoot.querySelector("#forecast-start").value = new Date().toISOString().slice(0, 10);
    const month = this.shadowRoot.querySelector("#summary-month");
    for (let value = 1; value <= 12; value++) {
      const option = document.createElement("option");
      option.value = value;
      option.textContent = new Intl.DateTimeFormat("fr-FR", { month: "long" }).format(new Date(2026, value - 1, 1));
      month.append(option);
    }
  }

  showPage(page) {
    const pages = {
      transactions: ["Opérations", "Consultez les mouvements enregistrés sur vos comptes."],
      summary: ["Synthèses", "Comparez les revenus et les dépenses par mois ou par année."],
      import: ["Import CSV", "Prévisualisez, vérifiez et confirmez l’import de vos relevés."],
      "category-rules": ["Règles", "Gérez vos catégories et les règles de catégorisation automatique."],
      planning: ["Planification", "Suivez vos budgets, échéances, soldes et objectifs d’épargne."]
    };
    if (!Object.hasOwn(pages, page)) page = "transactions";
    this.currentPage = page;
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
      this.renderBalanceTransactions();
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
        throw new Error("Impossible de charger les catégories, comptes ou règles.");
      }
      [this.categories, this.accounts, this.categoryRules] = await Promise.all([
        categoriesResponse.json(),
        accountsResponse.json(),
        rulesResponse.json()
      ]);
      this.renderPhase2Data();
      await this.loadSummary();
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    }
  }

  async loadPlanningData() {
    const feedback = this.shadowRoot.querySelector("#planning-feedback");
    const year = Number(this.shadowRoot.querySelector("#summary-year").value);
    const month = Number(this.shadowRoot.querySelector("#summary-month").value || new Date().getMonth() + 1);
    const firstDate = `${year}-${String(month).padStart(2, "0")}-01`;
    const lastDay = new Date(year, month, 0).getDate();
    const lastDate = `${year}-${String(month).padStart(2, "0")}-${String(lastDay).padStart(2, "0")}`;
    feedback.hidden = true;
    try {
      const [
        accountResponse,
        transactionResponse,
        budgetResponse,
        forecastResponse,
        occurrenceResponse,
        savingsGoalResponse
      ] = await Promise.all([
        fetch("/api/accounts"),
        fetch("/api/transactions"),
        fetch(`/api/planning/budgets?year=${year}&month=${month}`),
        fetch("/api/planning/forecasts"),
        fetch(`/api/planning/occurrences?startDate=${firstDate}&endDate=${lastDate}`),
        fetch(`/api/savings-goals?year=${year}&month=${month}`)
      ]);
      if (![accountResponse, transactionResponse, budgetResponse, forecastResponse, occurrenceResponse, savingsGoalResponse]
        .every(response => response.ok)) {
        throw new Error("Impossible de charger les données de planification.");
      }
      [
        this.accounts,
        this.transactions,
        this.planningBudgets,
        this.forecasts,
        this.occurrences,
        this.savingsGoals
      ] = await Promise.all([
        accountResponse.json(),
        transactionResponse.json(),
        budgetResponse.json(),
        forecastResponse.json(),
        occurrenceResponse.json(),
        savingsGoalResponse.json()
      ]);
      this.renderPlanningData();
      await this.loadBalance();
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    }
  }

  renderPlanningData() {
    this.renderCategoryOptions(this.shadowRoot.querySelector("#budget-category"));
    this.renderCategoryOptions(this.shadowRoot.querySelector("#forecast-category"), "Sans catégorie");
    for (const selector of ["#forecast-account", "#balance-account", "#savings-goal-account"]) {
      const select = this.shadowRoot.querySelector(selector);
      const selected = select.value;
      select.replaceChildren(this.option("", "Choisir un compte"));
      for (const account of this.accounts.filter(item => item.isActive)) {
        select.append(this.option(account.id, account.name));
      }
      if ([...select.options].some(option => option.value === selected)) select.value = selected;
    }
    this.renderBalanceTransactions();
    this.renderBudgets();
    this.renderForecasts();
    this.renderOccurrences();
    this.renderSavingsGoals();
  }

  renderBudgets() {
    const list = this.shadowRoot.querySelector("#budget-list");
    list.replaceChildren();
    for (const budget of this.planningBudgets) {
      const item = document.createElement("article");
      item.className = "planning-item";
      const summary = document.createElement("div");
      summary.className = "planning-item-summary";
      const title = document.createElement("strong");
      title.textContent = budget.categoryName;
      const status = document.createElement("span");
      status.textContent = `${this.formatCurrency(budget.expenseTotal)} dépensés · ${this.formatCurrency(budget.remainingAmount)} restants`;
      summary.append(title, status);
      const overrideForm = document.createElement("div");
      overrideForm.className = "budget-override";
      const amount = document.createElement("input");
      amount.type = "number";
      amount.min = "0.01";
      amount.step = "0.01";
      amount.value = budget.hasMonthlyOverride ? budget.effectiveAmount : "";
      amount.placeholder = `Récurrent : ${budget.recurringAmount}`;
      amount.setAttribute("aria-label", `Dérogation du mois pour ${budget.categoryName}`);
      const action = document.createElement("button");
      action.type = "button";
      action.className = "secondary-button";
      action.dataset.budgetOverride = budget.id;
      action.textContent = "Appliquer";
      overrideForm.append(amount, action);
      if (budget.hasMonthlyOverride) {
        const reset = document.createElement("button");
        reset.type = "button";
        reset.className = "secondary-button";
        reset.dataset.budgetReset = budget.id;
        reset.textContent = "Récurrent";
        overrideForm.append(reset);
      }
      item.append(summary, overrideForm);
      list.append(item);
    }
    if (this.planningBudgets.length === 0) {
      const empty = document.createElement("p");
      empty.className = "empty";
      empty.textContent = "Aucun budget récurrent défini.";
      list.append(empty);
    }
  }

  renderForecasts() {
    const list = this.shadowRoot.querySelector("#forecast-list");
    list.replaceChildren();
    for (const forecast of this.forecasts) {
      const item = document.createElement("article");
      item.className = "planning-item";
      const description = document.createElement("div");
      description.className = "planning-item-summary";
      const title = document.createElement("strong");
      title.textContent = forecast.name;
      const detail = document.createElement("span");
      detail.textContent = `${this.formatCurrency(forecast.amount)} · ${forecast.frequency === "Annual" ? "annuelle" : "mensuelle"} le ${forecast.dayOfMonth} · ${forecast.accountName} · libellé « ${forecast.labelPattern} »`;
      const status = document.createElement("span");
      status.textContent = `Prévision uniquement — ne modifie pas les opérations ni le solde constaté.`;
      description.append(title, detail, status);
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "secondary-button danger-button";
      remove.dataset.deleteForecast = forecast.id;
      remove.textContent = "Désactiver";
      item.append(description, remove);
      list.append(item);
    }
  }

  renderOccurrences() {
    const list = this.shadowRoot.querySelector("#occurrence-list");
    list.replaceChildren();
    for (const occurrence of this.occurrences ?? []) {
      const item = document.createElement("article");
      item.className = "planning-item";
      const text = document.createElement("div");
      text.className = "planning-item-summary";
      const title = document.createElement("strong");
      title.textContent = `${this.formatDate(occurrence.scheduledDate)} · ${occurrence.name}`;
      const details = document.createElement("span");
      details.textContent = occurrence.isCompleted
        ? `Réalisée : ${this.formatCurrency(occurrence.actualAmount)}`
        : `Prévue : ${this.formatCurrency(occurrence.forecastAmount)} · ${occurrence.accountName}`;
      text.append(title, details);
      item.append(text);
      list.append(item);
    }
    if ((this.occurrences ?? []).length === 0) {
      const empty = document.createElement("p");
      empty.className = "empty";
      empty.textContent = "Aucune échéance prévue ce mois.";
      list.append(empty);
    }
  }

  renderSavingsGoals() {
    const list = this.shadowRoot.querySelector("#savings-goal-list");
    list.replaceChildren();
    for (const goal of this.savingsGoals ?? []) {
      const item = document.createElement("article");
      item.className = "planning-item";
      const description = document.createElement("div");
      description.className = "planning-item-summary";
      const title = document.createElement("strong");
      title.textContent = goal.name;
      const details = document.createElement("span");
      details.textContent =
        `${goal.accountName} · cible ${this.formatCurrency(goal.targetAmount)} · ` +
        `${this.formatCurrency(goal.realizedMonthlyContribution)} réalisés · ` +
        `${this.formatCurrency(goal.plannedMonthlyContribution)} prévus ce mois`;
      const progress = document.createElement("span");
      progress.textContent =
        `${this.formatCurrency(goal.totalContributions)} de versements suivis · ${this.formatPercent(goal.progressPercent)} % de l’objectif`;
      description.append(title, details, progress);
      const actions = document.createElement("div");
      actions.className = "rule-actions";
      const edit = document.createElement("button");
      edit.type = "button";
      edit.className = "secondary-button";
      edit.dataset.editSavingsGoal = goal.id;
      edit.textContent = "Modifier";
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "secondary-button danger-button";
      remove.dataset.deleteSavingsGoal = goal.id;
      remove.textContent = "Désactiver";
      actions.append(edit, remove);
      item.append(description, actions);
      list.append(item);
    }
    if ((this.savingsGoals ?? []).length === 0) {
      const empty = document.createElement("p");
      empty.className = "empty";
      empty.textContent = "Aucun objectif d’épargne défini.";
      list.append(empty);
    }
  }

  renderBalanceTransactions() {
    const accountSelect = this.shadowRoot.querySelector("#balance-account");
    const transactionSelect = this.shadowRoot.querySelector("#balance-transaction");
    if (!accountSelect || !transactionSelect) return;
    const selected = transactionSelect.value;
    const accountId = accountSelect.value;
    transactionSelect.replaceChildren(this.option("", "Choisir une opération"));
    for (const transaction of this.transactions.filter(item => item.accountId === accountId)) {
      transactionSelect.append(this.option(
        transaction.id,
        `${this.formatDate(transaction.date)} · ${transaction.label} · ${this.formatCurrency(transaction.amount)}`
      ));
    }
    if ([...transactionSelect.options].some(option => option.value === selected)) transactionSelect.value = selected;
  }

  async createBudget(event) {
    event.preventDefault();
    await this.planningRequest("/api/planning/budgets", "POST", {
      categoryId: this.shadowRoot.querySelector("#budget-category").value,
      amount: Number(this.shadowRoot.querySelector("#budget-amount").value)
    }, "Budget récurrent ajouté.");
    this.shadowRoot.querySelector("#budget-form").reset();
  }

  async handleBudgetAction(event) {
    const button = event.target.closest("[data-budget-override], [data-budget-reset]");
    if (!button) return;
    const year = this.shadowRoot.querySelector("#summary-year").value;
    const month = this.shadowRoot.querySelector("#summary-month").value || String(new Date().getMonth() + 1);
    if (button.dataset.budgetOverride) {
      const amount = Number(button.parentElement.querySelector("input").value);
      if (!amount || amount <= 0) return;
      await this.planningRequest(
        `/api/planning/budgets/${button.dataset.budgetOverride}/overrides/${year}/${month}`,
        "PUT",
        { amount },
        "Dérogation budgétaire appliquée."
      );
    } else {
      await this.planningRequest(
        `/api/planning/budgets/${button.dataset.budgetReset}/overrides/${year}/${month}`,
        "DELETE",
        null,
        "Budget récurrent rétabli pour ce mois."
      );
    }
  }

  async createForecast(event) {
    event.preventDefault();
    const startDate = this.shadowRoot.querySelector("#forecast-start").value;
    const categoryId = this.shadowRoot.querySelector("#forecast-category").value || null;
    const succeeded = await this.planningRequest("/api/planning/forecasts", "POST", {
      name: this.shadowRoot.querySelector("#forecast-name").value.trim(),
      labelPattern: this.shadowRoot.querySelector("#forecast-pattern").value.trim(),
      accountId: this.shadowRoot.querySelector("#forecast-account").value,
      amount: Number(this.shadowRoot.querySelector("#forecast-amount").value),
      type: this.shadowRoot.querySelector("#forecast-type").value,
      dayOfMonth: Number(this.shadowRoot.querySelector("#forecast-day").value),
      frequency: this.shadowRoot.querySelector("#forecast-frequency").value,
      startDate,
      categoryId
    }, "Échéance prévisionnelle ajoutée.");
    if (succeeded) {
      this.shadowRoot.querySelector("#forecast-form").reset();
      this.shadowRoot.querySelector("#forecast-start").value = new Date().toISOString().slice(0, 10);
    }
  }

  async saveSavingsGoal(event) {
    event.preventDefault();
    const year = Number(this.shadowRoot.querySelector("#summary-year").value);
    const month = Number(this.shadowRoot.querySelector("#summary-month").value || new Date().getMonth() + 1);
    const succeeded = await this.planningRequest(
      this.editingSavingsGoalId ? `/api/savings-goals/${this.editingSavingsGoalId}` : "/api/savings-goals",
      this.editingSavingsGoalId ? "PUT" : "POST",
      {
        name: this.shadowRoot.querySelector("#savings-goal-name").value.trim(),
        accountId: this.shadowRoot.querySelector("#savings-goal-account").value,
        targetAmount: Number(this.shadowRoot.querySelector("#savings-goal-target").value),
        plannedMonthlyContribution: Number(this.shadowRoot.querySelector("#savings-goal-contribution").value),
        year,
        month
      },
      this.editingSavingsGoalId ? "Objectif d’épargne mis à jour." : "Objectif d’épargne créé."
    );
    if (succeeded) this.resetSavingsGoalForm();
  }

  async handleSavingsGoalAction(event) {
    const editButton = event.target.closest("[data-edit-savings-goal]");
    if (editButton) {
      const goal = this.savingsGoals.find(item => item.id === editButton.dataset.editSavingsGoal);
      if (!goal) return;
      this.editingSavingsGoalId = goal.id;
      this.shadowRoot.querySelector("#savings-goal-name").value = goal.name;
      this.shadowRoot.querySelector("#savings-goal-account").value = goal.accountId;
      this.shadowRoot.querySelector("#savings-goal-target").value = goal.targetAmount;
      this.shadowRoot.querySelector("#savings-goal-contribution").value = goal.plannedMonthlyContribution;
      this.shadowRoot.querySelector("#savings-goal-submit").textContent = "Enregistrer";
      this.shadowRoot.querySelector("#savings-goal-cancel").hidden = false;
      this.shadowRoot.querySelector("#savings-goal-name").focus();
      return;
    }

    const deleteButton = event.target.closest("[data-delete-savings-goal]");
    if (deleteButton) {
      await this.planningRequest(
        `/api/savings-goals/${deleteButton.dataset.deleteSavingsGoal}`,
        "DELETE",
        null,
        "Objectif d’épargne désactivé."
      );
    }
  }

  resetSavingsGoalForm() {
    this.editingSavingsGoalId = null;
    this.shadowRoot.querySelector("#savings-goal-form").reset();
    this.shadowRoot.querySelector("#savings-goal-submit").textContent = "Créer l’objectif";
    this.shadowRoot.querySelector("#savings-goal-cancel").hidden = true;
  }

  async handleForecastAction(event) {
    const button = event.target.closest("[data-delete-forecast]");
    if (!button) return;
    await this.planningRequest(`/api/planning/forecasts/${button.dataset.deleteForecast}`, "DELETE", null, "Échéance désactivée.");
  }

  async setBalanceReference(event) {
    event.preventDefault();
    await this.planningRequest(
      `/api/accounts/${this.shadowRoot.querySelector("#balance-account").value}/balances/reference`,
      "PUT",
      {
        transactionId: this.shadowRoot.querySelector("#balance-transaction").value,
        balanceAfterTransaction: Number(this.shadowRoot.querySelector("#reference-balance").value)
      },
      "Solde de référence enregistré."
    );
    await this.loadBalance();
  }

  async loadBalance() {
    const accountId = this.shadowRoot.querySelector("#balance-account")?.value;
    if (!accountId) return;
    try {
      const year = Number(this.shadowRoot.querySelector("#summary-year").value);
      const month = Number(this.shadowRoot.querySelector("#summary-month").value || new Date().getMonth() + 1);
      const lastDay = new Date(year, month, 0).getDate();
      const throughDate = `${year}-${String(month).padStart(2, "0")}-${String(lastDay).padStart(2, "0")}`;
      const response = await fetch(`/api/accounts/${accountId}/balances?throughDate=${throughDate}`);
      const result = await response.json();
      if (!response.ok) throw new Error(result.detail ?? result.title ?? "Impossible de charger le solde.");
      const output = this.shadowRoot.querySelector("#balance-result");
      if (result.actualBalance === null) {
        output.textContent = "Aucun solde de référence défini pour ce compte.";
      } else {
        output.textContent = `Solde constaté : ${this.formatCurrency(result.actualBalance)} · Solde prévisionnel (jusqu’au ${this.formatDate(result.throughDate)}) : ${this.formatCurrency(result.forecastBalance)}. L’opération de référence n’est pas recomptée.`;
      }
    } catch (exception) {
      const feedback = this.shadowRoot.querySelector("#planning-feedback");
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
    }
  }

  async planningRequest(url, method, body, successMessage) {
    const feedback = this.shadowRoot.querySelector("#planning-feedback");
    try {
      const options = { method };
      if (body) {
        options.headers = { "Content-Type": "application/json" };
        options.body = JSON.stringify(body);
      }
      const response = await fetch(url, options);
      const result = response.status === 204 ? null : await response.json();
      if (!response.ok) throw new Error(result?.detail ?? result?.title ?? "L'opération de planification a échoué.");
      await this.loadPlanningData();
      feedback.className = "feedback success";
      feedback.textContent = successMessage;
      feedback.hidden = false;
      return true;
    } catch (exception) {
      feedback.className = "feedback error";
      feedback.textContent = exception.message;
      feedback.hidden = false;
      return false;
    }
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
    const list = this.shadowRoot.querySelector("#rule-list");
    list.replaceChildren();
    this.shadowRoot.querySelector("#rules-count").textContent =
      `${this.categoryRules.length} règle${this.categoryRules.length === 1 ? "" : "s"}`;
    if (this.categoryRules.length === 0) {
      const empty = document.createElement("p");
      empty.className = "empty";
      empty.textContent = "Aucune règle active. Créez-en une pour suggérer automatiquement une catégorie lors des imports.";
      list.append(empty);
      return;
    }
    for (const rule of this.categoryRules) {
      const row = document.createElement("article");
      row.className = "rule-item";
      const description = document.createElement("div");
      const name = document.createElement("strong");
      name.textContent = rule.name;
      const details = document.createElement("span");
      const category = this.categories.find(item => item.id === rule.categoryId)?.name ?? "Catégorie indisponible";
      const subcategory = rule.subcategoryName ? ` · Sous-catégorie : ${rule.subcategoryName}` : "";
      details.textContent = `Motif « ${rule.pattern} » · ${category}${subcategory}`;
      description.append(name, details);
      const actions = document.createElement("div");
      actions.className = "rule-actions";
      const edit = document.createElement("button");
      edit.type = "button";
      edit.className = "secondary-button";
      edit.dataset.editRule = rule.id;
      edit.textContent = "Modifier";
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "secondary-button danger-button";
      remove.dataset.deleteRule = rule.id;
      remove.textContent = "Désactiver";
      actions.append(edit, remove);
      row.append(description, actions);
      list.append(row);
    }
  }

  renderCategories() {
    const list = this.shadowRoot.querySelector("#category-list");
    list.replaceChildren();
    if (this.categories.length === 0) return;
    const heading = document.createElement("h3");
    heading.textContent = "Catégories actives";
    list.append(heading);
    for (const category of this.categories.filter(item => item.isActive)) {
      const row = document.createElement("article");
      row.className = "category-item";
      const name = document.createElement("strong");
      name.textContent = category.name;
      const actions = document.createElement("div");
      actions.className = "rule-actions";
      const edit = document.createElement("button");
      edit.type = "button";
      edit.className = "secondary-button";
      edit.dataset.editCategory = category.id;
      edit.textContent = "Renommer";
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "secondary-button danger-button";
      remove.dataset.deleteCategory = category.id;
      remove.textContent = "Désactiver";
      actions.append(edit, remove);
      row.append(name, actions);
      list.append(row);
    }
  }

  async saveCategoryRule(event) {
    event.preventDefault();
    const feedback = this.shadowRoot.querySelector("#rules-feedback");
    const button = this.shadowRoot.querySelector("#rule-submit");
    button.disabled = true;
    try {
      const rule = {
        name: this.shadowRoot.querySelector("#rule-name").value.trim(),
        pattern: this.shadowRoot.querySelector("#rule-pattern").value.trim(),
        categoryId: this.shadowRoot.querySelector("#rule-category").value,
        subcategoryName: this.shadowRoot.querySelector("#rule-subcategory").value.trim() || null
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
      if (!response.ok) throw new Error(result?.detail ?? result?.title ?? "Impossible d'enregistrer la règle.");
      feedback.className = "feedback success";
      feedback.textContent = isEditing ? "Règle mise à jour." : "Règle créée.";
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
    this.shadowRoot.querySelector("#rule-submit").textContent = "Créer la règle";
    this.shadowRoot.querySelector("#rule-cancel").hidden = true;
  }

  async handleRuleAction(event) {
    const editButton = event.target.closest("[data-edit-rule]");
    if (editButton) {
      const rule = this.categoryRules.find(item => item.id === editButton.dataset.editRule);
      if (!rule) return;
      this.editingRuleId = rule.id;
      this.shadowRoot.querySelector("#rule-name").value = rule.name;
      this.shadowRoot.querySelector("#rule-pattern").value = rule.pattern;
      this.shadowRoot.querySelector("#rule-category").value = rule.categoryId;
      this.shadowRoot.querySelector("#rule-subcategory").value = rule.subcategoryName ?? "";
      this.shadowRoot.querySelector("#rule-submit").textContent = "Enregistrer";
      this.shadowRoot.querySelector("#rule-cancel").hidden = false;
      this.shadowRoot.querySelector("#rule-name").focus();
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
        throw new Error(problem.detail ?? problem.title ?? "Impossible de désactiver la règle.");
      }
      feedback.className = "feedback success";
      feedback.textContent = "Règle désactivée.";
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
        throw new Error(problem.detail ?? problem.title ?? "Impossible de désactiver la catégorie.");
      }
      feedback.className = "feedback success";
      feedback.textContent = "Catégorie désactivée.";
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
    const details = this.shadowRoot.querySelector("#summary-details");
    details.replaceChildren();
    details.hidden = true;
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
    summary.categories.forEach((category, index) => {
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
      detailsButton.className = "text-button";
      detailsButton.dataset.summaryCategory = index;
      detailsButton.textContent = "Opérations";
      actionCell.append(detailsButton);
      row.append(actionCell);
      body.append(row);
    });
    body.onclick = event => {
      const button = event.target.closest("[data-summary-category]");
      if (button) this.renderSummaryDetails(summary.categories[Number(button.dataset.summaryCategory)]);
    };
  }

  renderSummaryDetails(category) {
    const details = this.shadowRoot.querySelector("#summary-details");
    details.replaceChildren();
    const title = document.createElement("h3");
    title.textContent = `${category.categoryName} · ${category.transactions.length} opération${category.transactions.length === 1 ? "" : "s"}`;
    const table = document.createElement("table");
    const head = document.createElement("thead");
    const headerRow = document.createElement("tr");
    for (const label of ["Date", "Opération", "Compte", "Type", "Montant"]) {
      const th = document.createElement("th");
      th.textContent = label;
      headerRow.append(th);
    }
    head.append(headerRow);
    const body = document.createElement("tbody");
    for (const transaction of category.transactions) body.append(this.transactionRow(transaction));
    table.append(head, body);
    details.append(title, table);
    details.hidden = false;
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

    this.shadowRoot.querySelector("#preview-count").textContent = `${preview.transactions.length} ligne${preview.transactions.length === 1 ? "" : "s"} valide${preview.transactions.length === 1 ? "" : "s"}`;
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
      const commitMessage = `${result.importedCount} opération${result.importedCount === 1 ? "" : "s"} ajoutée${result.importedCount === 1 ? "" : "s"}, ${result.excludedCount} exclue${result.excludedCount === 1 ? "" : "s"}, ${result.duplicateCount} doublon${result.duplicateCount === 1 ? "" : "s"} ignoré${result.duplicateCount === 1 ? "" : "s"}, ${result.errors.length} erreur${result.errors.length === 1 ? "" : "s"}.`;
      feedback.className = "feedback success";
      feedback.textContent = commitMessage;
      feedback.hidden = false;
      await this.loadTransactions();
      this.phase2DataPromise = this.loadPhase2Data();
      await this.phase2DataPromise;
      await this.loadPlanningData();
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
      const forecastMatch = row.querySelector("[data-forecast-match]")?.value;
      const transferMatch = row.querySelector("[data-transfer-match]")?.value;
      const amount = Number(row.querySelector("[data-amount]").value.replace(",", "."));
      const subcategoryName = row.querySelector("[data-subcategory]").value.trim();
      return {
        rowNumber: Number(row.dataset.rowNumber),
        include: row.querySelector("[data-include]").checked,
        transactionDate: row.dataset.transactionDate,
        label: row.dataset.label,
        amount: Math.abs(amount),
        type: amount < 0 ? "Expense" : "Income",
        categoryName: newCategoryInput.hidden ? categorySelect.value : newCategoryInput.value.trim(),
        subcategoryName,
        createSubcategoryRule: row.dataset.createSubcategoryRule === "true" && Boolean(subcategoryName),
        confirmForecastMatch: Boolean(forecastMatch),
        forecastId: forecastMatch ? JSON.parse(forecastMatch).forecastId : null,
        forecastDate: forecastMatch ? JSON.parse(forecastMatch).scheduledDate : null,
        confirmTransferMatch: Boolean(transferMatch),
        transferTransactionId: transferMatch ? JSON.parse(transferMatch).transactionId : null,
        transferRowNumber: transferMatch ? JSON.parse(transferMatch).rowNumber : null
      };
    });
  }

  transactionLabelCell(transaction) {
    const cell = document.createElement("td");
    const label = document.createElement("span");
    label.textContent = transaction.label;
    cell.append(label);
    if (transaction.subcategoryName) {
      const subcategory = document.createElement("small");
      subcategory.className = "transaction-subcategory";
      subcategory.textContent = transaction.subcategoryName;
      cell.append(subcategory);
    }
    return cell;
  }

  transactionRow(transaction) {
    const row = document.createElement("tr");
    row.append(
      this.cell(this.formatDate(transaction.date), "date"),
      this.transactionLabelCell(transaction),
      this.cell(transaction.accountName ?? transaction.accountLabel ?? "—"),
      this.typeCell(transaction.type, transaction.isTransfer),
      this.cell(this.formatAmount(transaction.amount, transaction.type), "amount")
    );
    return row;
  }

  previewRow(transaction) {
    const row = document.createElement("tr");
    row.dataset.rowNumber = transaction.rowNumber;
    row.dataset.transactionDate = transaction.transactionDate;
    row.dataset.label = transaction.label;
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
    const newCategoryInput = document.createElement("input");
    newCategoryInput.type = "text";
    newCategoryInput.maxLength = 100;
    newCategoryInput.placeholder = "Nom de la nouvelle catégorie";
    newCategoryInput.required = false;
    newCategoryInput.hidden = true;
    newCategoryInput.dataset.newCategory = "";
    newCategoryInput.setAttribute("aria-label", `Nouvelle catégorie de l'opération, ligne ${transaction.rowNumber}`);
    const subcategoryInput = document.createElement("input");
    subcategoryInput.type = "text";
    subcategoryInput.maxLength = 200;
    subcategoryInput.placeholder = "Sous-catégorie (facultatif)";
    subcategoryInput.value = transaction.subcategoryName ?? "";
    subcategoryInput.dataset.subcategory = "";
    subcategoryInput.setAttribute("aria-label", `Sous-catégorie de l'opération, ligne ${transaction.rowNumber}`);
    const categoryContent = document.createElement("div");
    categoryContent.className = "preview-category";
    categoryContent.append(categorySelect, newCategoryInput, subcategoryInput);
    if (transaction.categorySuggestion) {
      const suggestion = document.createElement("small");
      suggestion.className = "category-suggestion";
      suggestion.textContent = `Suggestion · ${transaction.categorySuggestion.ruleName} (${transaction.categorySuggestion.pattern})`;
      categoryContent.append(suggestion);
    }
    if (transaction.forecastMatchCandidates?.length) {
      const forecastSelect = document.createElement("select");
      forecastSelect.dataset.forecastMatch = "";
      forecastSelect.setAttribute("aria-label", `Rapprochement d'échéance, ligne ${transaction.rowNumber}`);
      forecastSelect.append(this.option("", "Échéance à rapprocher ?"));
      for (const candidate of transaction.forecastMatchCandidates) {
        const label = `${candidate.forecastName} · ${this.formatDate(candidate.scheduledDate)} · prévu ${this.formatCurrency(candidate.forecastAmount)} · écart ${this.formatCurrency(candidate.amountDifference)}`;
        const option = this.option(JSON.stringify({
          forecastId: candidate.forecastId,
          scheduledDate: candidate.scheduledDate
        }), label);
        forecastSelect.append(option);
      }
      const matchNote = document.createElement("small");
      matchNote.className = "forecast-suggestion";
      const firstCandidate = transaction.forecastMatchCandidates[0];
      matchNote.textContent = firstCandidate.isAmbiguous
        ? "Plusieurs échéances correspondent — choisissez explicitement."
        : `Même compte et libellé proche; écart de montant : ${this.formatCurrency(firstCandidate.amountDifference)}. Confirmez pour marquer l'échéance réalisée.`;
      categoryContent.append(forecastSelect, matchNote);
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
      this.cell(transaction.label, "preview-label"),
      this.cell(`${accountName} · ${transaction.accountNumber}`),
      this.controlCell(categoryContent, "category"),
      this.controlCell(amountInput, "review-amount"),
      this.controlCell(includeControl, "review-include")
    );
    return row;
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
    const label = sourceRow.dataset.label.trim();
    if (!label) return;

    const normalizedLabel = label.toLocaleLowerCase("fr-FR");
    const categorySelect = sourceRow.querySelector("[data-category]");
    const newCategoryInput = sourceRow.querySelector("[data-new-category]");
    const categoryName = newCategoryInput.hidden ? categorySelect.value : newCategoryInput.value.trim();
    const subcategoryName = sourceRow.querySelector("[data-subcategory]").value.trim();
    const createSubcategoryRule = sourceRow.dataset.createSubcategoryRule === "true";
    this.syncingPreviewRows = true;
    try {
      for (const row of this.shadowRoot.querySelectorAll("#preview-body tr")) {
        if (row === sourceRow
            || row.dataset.label.trim().toLocaleLowerCase("fr-FR") !== normalizedLabel) {
          continue;
        }

        const targetCategory = row.querySelector("[data-category]");
        const targetNewCategory = row.querySelector("[data-new-category]");
        targetNewCategory.hidden = true;
        targetNewCategory.required = false;
        targetNewCategory.value = "";
        if (categoryName && ![...targetCategory.options].some(option => option.value === categoryName)) {
          targetCategory.insertBefore(this.option(categoryName, categoryName), targetCategory.querySelector("[data-add-category]"));
        }
        targetCategory.value = categoryName;
        row.querySelector("[data-subcategory]").value = subcategoryName;
        row.dataset.createSubcategoryRule = createSubcategoryRule && subcategoryName ? "true" : "false";
      }
    } finally {
      this.syncingPreviewRows = false;
    }
  }

  formatReviewAmount(amount, type) {
    const sign = type === 0 || type === "Expense" ? "-" : "+";
    return `${sign}${new Intl.NumberFormat("fr-FR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount)}`;
  }

  updateReviewAmount(input) {
    const value = input.value.trim().replace(",", ".");
    const amount = Number(value);
    const isValid = /^[+-]?(?:\d+|\d*\.\d{1,2})$/.test(value) && Number.isFinite(amount) && amount !== 0;
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

  formatPercent(value) {
    return new Intl.NumberFormat("fr-FR", { maximumFractionDigits: 1 }).format(value);
  }
}

customElements.define("konta-app", KontaApp);