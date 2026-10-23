import { Component, Input, Output, EventEmitter } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Dataset } from "./types";
@Component({
  selector: "dg-upload",
  standalone: true,
  imports: [CommonModule],
  template: `<label
    class="upload-box"
    [class.uploaded]="dataset"
    [class.disabled]="disabled"
    (dragover)="drag($event)"
    (drop)="drop($event)"
    ><input
      type="file"
      accept=".csv"
      [disabled]="disabled"
      (change)="pick($event)"
    /><span class="upload-symbol">{{ dataset ? "✓" : "↑" }}</span>
    <div>
      <span class="upload-role">{{
        role === "baseline" ? "REFERENCE DATA" : "INCOMING DATA"
      }}</span>
      <h3>
        {{ role === "baseline" ? "Baseline dataset" : "Current dataset" }}
      </h3>
      <p>{{ dataset?.fileName || "Drop a CSV here, or browse files" }}</p>
      <small *ngIf="dataset"
        >{{ dataset.profile.rowCount | number }} rows ·
        {{ dataset.profile.columnCount }} columns · Click to replace</small
      >
    </div></label
  >`,
})
export class UploadComponent {
  @Input() role = "baseline";
  @Input() dataset: Dataset | null = null;
  @Input() disabled = false;
  @Output() picked = new EventEmitter<File>();
  pick(e: Event) {
    const input = e.target as HTMLInputElement;
    const f = input.files?.[0];
    if (f) this.picked.emit(f);
    input.value = "";
  }
  drag(e: DragEvent) {
    e.preventDefault();
  }
  drop(e: DragEvent) {
    e.preventDefault();
    if (!this.disabled && e.dataTransfer?.files[0])
      this.picked.emit(e.dataTransfer.files[0]);
  }
}
