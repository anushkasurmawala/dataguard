import { bootstrapApplication } from "@angular/platform-browser";
import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  provideHttpClient,
  HttpClient,
  HttpErrorResponse,
} from "@angular/common/http";
import { firstValueFrom } from "rxjs";
import { UploadComponent } from "./upload";
import { ProfileComponent } from "./profile";
import { FeatureViewComponent } from "./feature-view";
import { Dataset, Comparison, FeatureResult } from "./types";
@Component({
  selector: "app-root",
  standalone: true,
  imports: [
    CommonModule,
    UploadComponent,
    ProfileComponent,
    FeatureViewComponent,
  ],
  template: ` <header>
      <a class="brand" href="/" aria-label="DataGuard home"
        ><span class="logo">◈</span> DATAGUARD</a
      ><span class="header-line"></span
      ><span class="header-caption">Dataset quality & drift monitoring</span
      ><span class="prototype"><span></span> Internship prototype</span>
    </header>
    <main>
      <div class="eyebrow">DATA OBSERVABILITY / COMPARE</div>
      <div class="page-title">
        <div>
          <h1>Know what changed.</h1>
          <p>
            Compare two datasets. Understand quality changes and numerical
            drift.
          </p>
        </div>
        <span class="version">PROTOTYPE v1.0</span>
      </div>
      <section class="panel upload-panel">
        <div class="section-heading">
          <h2><span class="step">01</span> Your datasets</h2>
          <span>CSV · Up to 20 MB per file</span>
        </div>
        <div class="uploads">
          <dg-upload
            role="baseline"
            [dataset]="baseline"
            [disabled]="busy"
            (picked)="upload($event, 'baseline')"
          ></dg-upload
          ><span class="compare-arrow">→</span
          ><dg-upload
            role="current"
            [dataset]="current"
            [disabled]="busy"
            (picked)="upload($event, 'current')"
          ></dg-upload>
        </div>
        <div class="upload-actions">
          <div>
            <button
              class="text-button"
              (click)="loadSamples()"
              [disabled]="busy"
            >
              ↗ Try the sample datasets</button
            ><span class="muted"
              >Synthetic customer data, analysed by the API</span
            >
          </div>
          <button
            class="primary"
            (click)="compare()"
            [disabled]="busy || !baseline || !current"
          >
            {{ busy ? activity : "Compare datasets" }} <span>→</span>
          </button>
        </div>
      </section>
      <div class="error" role="alert" *ngIf="error">{{ error }}</div>
      <div class="live-note" aria-live="polite" *ngIf="busy">
        {{ activity }}…
      </div>
      <div class="profile-pair" *ngIf="baseline || current">
        <dg-profile *ngIf="baseline" [dataset]="baseline"></dg-profile
        ><dg-profile *ngIf="current" [dataset]="current"></dg-profile>
      </div>
      <ng-container *ngIf="result as r"
        ><div class="results-heading">
          <div>
            <div class="eyebrow">COMPARISON RESULTS</div>
            <h2>Change, brought into focus.</h2>
          </div>
          <span class="computed">✓ Calculated from your files</span>
        </div>
        <div class="metrics">
          <article>
            <span>Baseline rows</span
            ><strong>{{ r.baseline.profile.rowCount | number }}</strong
            ><small>{{ r.baseline.fileName }}</small>
          </article>
          <article>
            <span>Current rows</span
            ><strong>{{ r.current.profile.rowCount | number }}</strong
            ><small
              >{{
                signed(r.current.profile.rowCount - r.baseline.profile.rowCount)
              }}
              rows from baseline</small
            >
          </article>
          <article>
            <span>Features with PSI</span><strong>{{ numericCount }}</strong
            ><small>{{ r.features.length }} common features</small>
          </article>
          <article>
            <span>Distribution change</span>
            <div class="status-counts">
              <b class="stable">{{ count("Stable") }}<small>Stable</small></b
              ><b class="moderate"
                >{{ count("Moderate") }}<small>Moderate</small></b
              ><b class="significant"
                >{{ count("Significant") }}<small>Significant</small></b
              >
            </div>
          </article>
        </div>
        <section class="panel schema">
          <h2>
            Schema changes
            <span class="count">{{ r.schemaChanges.length }}</span>
          </h2>
          <p *ngIf="!r.schemaChanges.length" class="muted">
            No added, removed, or type-changed columns.
          </p>
          <div class="schema-items">
            <div *ngFor="let s of r.schemaChanges">
              <span class="tag">{{ s.changeType }}</span
              ><code>{{ s.columnName }}</code
              ><small *ngIf="s.changeType === 'Type changed'"
                >{{ s.baselineType }} → {{ s.currentType }}</small
              >
            </div>
          </div>
        </section>
        <section class="panel feature-table">
          <div class="section-heading">
            <h2><span class="step">02</span> Feature comparison</h2>
            <span>Missing change in percentage points</span>
          </div>
          <div class="filters">
            <button
              *ngFor="let f of filters"
              [class.active]="filter === f"
              (click)="filter = f"
            >
              {{ f }}
              <span>{{ f === "All" ? r.features.length : count(f) }}</span>
            </button>
          </div>
          <div class="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>FEATURE</th>
                  <th>PSI</th>
                  <th>MISSING CHANGE</th>
                  <th>STATUS</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr
                  *ngFor="let f of visibleFeatures"
                  [class.selected]="selected?.featureName === f.featureName"
                >
                  <td>
                    <button class="feature-link" (click)="selected = f">
                      {{ f.featureName }}</button
                    ><small *ngIf="f.exclusionReason">{{
                      f.exclusionReason
                    }}</small>
                  </td>
                  <td class="mono">
                    {{ f.drift ? (f.drift.psi | number: "1.3-3") : "—" }}
                  </td>
                  <td class="mono">{{ signed(f.missingChange, 2) }} pp</td>
                  <td>
                    <span
                      class="badge"
                      [ngClass]="f.drift?.status?.toLowerCase() || 'neutral'"
                      >{{ f.drift?.status || "Not evaluated" }}</span
                    >
                  </td>
                  <td>
                    <button
                      class="inspect"
                      (click)="selected = f"
                      [attr.aria-label]="'Inspect ' + f.featureName"
                    >
                      ↗
                    </button>
                  </td>
                </tr>
                <tr *ngIf="!visibleFeatures.length">
                  <td colspan="5" class="muted">
                    No features in this category.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
        <dg-feature-view
          *ngIf="selected"
          [feature]="selected"
        ></dg-feature-view>
        <div class="interpretation">
          <b>Reading these results</b>
          <p>
            PSI measures input distribution changes. Stable &lt; 0.10 · Moderate
            0.10–0.25 · Significant ≥ 0.25. These are monitoring conventions.
            Drift alone does not establish a change in model accuracy.
          </p>
        </div></ng-container
      >
      <section class="empty-state" *ngIf="!result">
        <span class="empty-icon">▥</span>
        <h2>Every comparison starts with a baseline.</h2>
        <p>
          Upload your reference and incoming CSV files to see what’s changed.<br />Quality
          metrics, schema checks, and feature distributions — in one view.
        </p>
        <div>
          <span>01 Upload</span><i>—</i><span>02 Compare</span><i>—</i
          ><span>03 Explore</span>
        </div>
      </section>
      <footer>
        <span>DATAGUARD <b>/</b> A practical dataset monitoring prototype</span
        ><span>Angular + ASP.NET Core + PostgreSQL</span>
      </footer>
    </main>`,
})
export class AppComponent {
  baseline: Dataset | null = null;
  current: Dataset | null = null;
  result: Comparison | null = null;
  selected: FeatureResult | null = null;
  busy = false;
  activity = "";
  error = "";
  filter = "All";
  filters = ["All", "Stable", "Moderate", "Significant"];
  constructor(private http: HttpClient) {}
  signed(n: number, d = 0) {
    return (n > 0 ? "+" : "") + n.toFixed(d);
  }
  count(status: string) {
    return (
      this.result?.features.filter((f) => f.drift?.status === status).length ||
      0
    );
  }
  get numericCount() {
    return this.result?.features.filter((f) => f.drift).length || 0;
  }
  get visibleFeatures() {
    return (
      this.result?.features.filter(
        (f) => this.filter === "All" || f.drift?.status === this.filter,
      ) || []
    );
  }
  fail(e: unknown) {
    this.error =
      e instanceof HttpErrorResponse
        ? e.error?.message ||
          e.error?.title ||
          "Cannot reach the API. Make sure the backend is running."
        : "The request could not be completed.";
  }
  async send(file: File, role: "baseline" | "current") {
    const form = new FormData();
    form.append("file", file);
    return firstValueFrom(
      this.http.post<Dataset>("/api/datasets/" + role, form),
    );
  }
  async upload(file: File, role: "baseline" | "current") {
    this.busy = true;
    this.activity = "Profiling " + role;
    this.error = "";
    this.result = null;
    this.selected = null;
    this[role] = null;
    try {
      this[role] = await this.send(file, role);
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
    }
  }
  async loadSamples() {
    this.busy = true;
    this.activity = "Profiling sample datasets";
    this.error = "";
    this.result = null;
    this.selected = null;
    this.baseline = null;
    this.current = null;
    try {
      for (const role of ["baseline", "current"] as const) {
        const response = await fetch("/samples/customer_" + role + ".csv");
        if (!response.ok) throw new Error("Sample unavailable");
        const file = new File(
          [await response.blob()],
          "customer_" + role + ".csv",
          { type: "text/csv" },
        );
        this[role] = await this.send(file, role);
      }
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
    }
  }
  async compare() {
    if (!this.baseline || !this.current) return;
    this.busy = true;
    this.activity = "Comparing datasets";
    this.error = "";
    try {
      this.result = await firstValueFrom(
        this.http.post<Comparison>("/api/comparisons", {
          baselineDatasetId: this.baseline.id,
          currentDatasetId: this.current.id,
        }),
      );
      this.selected =
        this.result.features.find((f) => f.drift) ||
        this.result.features[0] ||
        null;
      this.filter = "All";
    } catch (e) {
      this.fail(e);
    } finally {
      this.busy = false;
    }
  }
}
bootstrapApplication(AppComponent, { providers: [provideHttpClient()] }).catch(
  console.error,
);
