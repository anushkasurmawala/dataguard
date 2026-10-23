import { Component, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Dataset } from "./types";
@Component({
  selector: "dg-profile",
  standalone: true,
  imports: [CommonModule],
  template: `<details class="panel profile" *ngIf="dataset as d">
    <summary>
      <span
        ><b>{{ d.datasetType | titlecase }} profile</b
        ><small>{{ d.fileName }}</small></span
      ><span>Inspect quality <b>+</b></span>
    </summary>
    <div class="profile-stats">
      <div>
        <strong>{{ d.profile.rowCount | number }}</strong
        ><small>Rows</small>
      </div>
      <div>
        <strong>{{ d.profile.duplicateRowCount | number }}</strong
        ><small
          >Duplicate rows ({{
            d.profile.duplicatePercentage | number: "1.1-1"
          }}%)</small
        >
      </div>
      <div>
        <strong>{{ d.profile.totalMissingValues | number }}</strong
        ><small
          >Missing cells ({{
            d.profile.overallMissingPercentage | number: "1.1-1"
          }}%)</small
        >
      </div>
    </div>
    <div class="table-scroll">
      <table>
        <thead>
          <tr>
            <th>Feature</th>
            <th>Type</th>
            <th>Missing</th>
            <th>Unique</th>
            <th>Most frequent category</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let f of d.profile.features">
            <td>{{ f.featureName }}</td>
            <td>{{ f.dataType }}</td>
            <td>{{ f.missingPercentage | number: "1.1-1" }}%</td>
            <td>{{ f.uniqueCount }}</td>
            <td>{{ top(f.categories) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <details *ngFor="let f of d.profile.features">
      <summary>
        {{ f.featureName }} ·
        {{ f.numeric ? "Numerical statistics" : "Category frequencies" }}
      </summary>
      <p *ngIf="f.numeric as n">
        Mean {{ n.mean | number: "1.2-2" }} · Median
        {{ n.median | number: "1.2-2" }} · SD
        {{ n.standardDeviation | number: "1.2-2" }} · Min {{ n.minimum }} · Max
        {{ n.maximum }} · Q1 {{ n.q1 }} · Q3 {{ n.q3 }} · Outliers
        {{ n.outlierPercentage | number: "1.2-2" }}%
      </p>
      <p *ngFor="let category of f.categories | keyvalue">
        {{ category.key }}: {{ category.value }}
      </p>
    </details>
  </details>`,
})
export class ProfileComponent {
  @Input() dataset: Dataset | null = null;
  top(c: Record<string, number> | null) {
    if (!c) return "—";
    const entry = Object.entries(c).sort((a, b) => b[1] - a[1])[0];
    return entry ? entry[0] + " (" + entry[1] + ")" : "—";
  }
}
