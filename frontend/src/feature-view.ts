import {
  Component,
  Input,
  ViewChild,
  ElementRef,
  AfterViewInit,
  OnChanges,
  OnDestroy,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { Chart, registerables } from "chart.js";
import { FeatureResult } from "./types";
Chart.register(...registerables);
@Component({
  selector: "dg-feature-view",
  standalone: true,
  imports: [CommonModule],
  template: `<section class="panel detail">
    <div class="section-heading">
      <div>
        <div class="eyebrow">FEATURE DETAIL</div>
        <h2>{{ feature.featureName }}</h2>
      </div>
      <span
        class="badge"
        [ngClass]="feature.drift?.status?.toLowerCase() || 'neutral'"
        >{{ feature.drift?.status || "Not evaluated" }}</span
      >
    </div>
    <div class="detail-layout">
      <div>
        <h3>Baseline vs. current distribution</h3>
        <p class="muted">
          Baseline-derived bins · non-missing observations (%)
        </p>
        <div class="chart">
          <canvas
            #canvas
            aria-label="Baseline and current feature distribution"
            role="img"
          ></canvas>
        </div>
        <p *ngIf="!feature.drift" class="muted">
          {{ feature.exclusionReason }}. Quality measurements are still
          available.
        </p>
      </div>
      <div class="statistics">
        <table>
          <thead>
            <tr>
              <th>METRIC</th>
              <th>BASELINE</th>
              <th>CURRENT</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>Missing</td>
              <td>
                {{ feature.baselineMissingPercentage | number: "1.2-2" }}%
              </td>
              <td>{{ feature.currentMissingPercentage | number: "1.2-2" }}%</td>
            </tr>
            <tr *ngFor="let stat of stats">
              <td>{{ stat.label }}</td>
              <td>{{ value("baseline", stat.key) }}</td>
              <td>{{ value("current", stat.key) }}</td>
            </tr>
            <tr>
              <td>PSI</td>
              <td colspan="2">
                {{
                  feature.drift ? (feature.drift.psi | number: "1.4-4") : "—"
                }}
              </td>
            </tr>
          </tbody>
        </table>
        <small
          >Outliers use each dataset’s own 1.5 × IQR bounds. Unusual
          observations are not necessarily errors.</small
        >
      </div>
    </div>
  </section>`,
})
export class FeatureViewComponent
  implements AfterViewInit, OnChanges, OnDestroy
{
  @Input({ required: true }) feature!: FeatureResult;
  @ViewChild("canvas") canvas!: ElementRef<HTMLCanvasElement>;
  chart: Chart | null = null;
  stats = [
    { label: "Mean", key: "mean" },
    { label: "Median", key: "median" },
    { label: "Std. deviation", key: "standardDeviation" },
    { label: "Minimum", key: "minimum" },
    { label: "Maximum", key: "maximum" },
    { label: "Q1", key: "q1" },
    { label: "Q3", key: "q3" },
    { label: "Outliers (%)", key: "outlierPercentage" },
  ];
  value(role: "baseline" | "current", key: string) {
    const stats =
      this.feature[
        role === "baseline" ? "baselineStatistics" : "currentStatistics"
      ];
    return stats
      ? (stats as unknown as Record<string, number>)[key].toLocaleString(
          undefined,
          { maximumFractionDigits: 2 },
        )
      : "—";
  }
  ngAfterViewInit() {
    this.draw();
  }
  ngOnChanges() {
    if (this.canvas) this.draw();
  }
  draw() {
    this.chart?.destroy();
    const bins = this.feature.drift?.bins || [];
    this.chart = new Chart(this.canvas.nativeElement, {
      type: "bar",
      data: {
        labels: bins.map((b) => b.label),
        datasets: [
          {
            label: "Baseline",
            data: bins.map((b) => b.baselinePercentage),
            backgroundColor: "#0d8177",
            borderRadius: 3,
          },
          {
            label: "Current",
            data: bins.map((b) => b.currentPercentage),
            backgroundColor: "#91b7e9",
            borderRadius: 3,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: "bottom",
            labels: { usePointStyle: true, boxWidth: 8 },
          },
        },
        scales: {
          y: {
            beginAtZero: true,
            title: { display: true, text: "Observations (%)" },
          },
          x: {
            grid: { display: false },
            ticks: { maxRotation: 40, font: { size: 10 } },
          },
        },
      },
    });
  }
  ngOnDestroy() {
    this.chart?.destroy();
  }
}
