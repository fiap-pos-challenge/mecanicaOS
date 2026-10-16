import { DialogRef } from '@angular/cdk/dialog';
import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { isApiError } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiDialogLayout } from '../../../shared/ui/dialog/dialog';
import { CustomerJobsGateway, Job } from '../data/customer-jobs.gateway';
import { downloadText, pollJob } from '../data/job-polling';

export const MAX_IMPORT_BYTES = 2 * 1024 * 1024;
export const IMPORT_TEMPLATE =
  'nome;documento;telefone;email\nMaria Souza;529.982.247-25;(11) 98765-4321;maria@exemplo.com\n';

@Component({
  selector: 'app-import-customers-dialog',
  imports: [UiButton, UiDialogLayout],
  templateUrl: './import-customers-dialog.html',
  styleUrl: './dialog-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportCustomersDialog {
  protected readonly ref = inject(DialogRef);
  private readonly jobs = inject(CustomerJobsGateway);
  private readonly document = inject(DOCUMENT);
  private polling: Subscription | null = null;

  protected readonly text = messages.customers.import;
  protected readonly file = signal<File | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly running = signal(false);
  protected readonly job = signal<Job | null>(null);

  constructor() {
    inject(DestroyRef).onDestroy(() => this.polling?.unsubscribe());
  }

  protected downloadTemplate(): void {
    downloadText(this.document, 'modelo-clientes.csv', IMPORT_TEMPLATE, 'text/csv;charset=utf-8');
  }

  protected choose(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.error.set(null);
    this.job.set(null);
    if (file && file.size > MAX_IMPORT_BYTES) {
      this.file.set(null);
      this.error.set(this.text.tooLarge);
      return;
    }

    this.file.set(file);
  }

  protected async submit(): Promise<void> {
    const file = this.file();
    if (!file || !file.name.toLowerCase().endsWith('.csv')) {
      this.error.set(this.text.chooseFile);
      return;
    }

    this.running.set(true);
    this.error.set(null);
    try {
      const csv = await file.text();
      this.jobs.importCustomers(file.name, csv).subscribe({
        next: (job) => {
          this.job.set(job);
          this.watch(job.id);
        },
        error: (error: unknown) => this.fail(error),
      });
    } catch (error) {
      this.fail(error);
    }
  }

  private watch(id: string): void {
    this.polling = pollJob((jobId) => this.jobs.job(jobId), id).subscribe({
      next: (job) => {
        this.job.set(job);
        if (job.status === 'Completed' || job.status === 'Failed') {
          this.running.set(false);
          if (job.status === 'Failed') {
            this.error.set(this.text.failed);
          }
        }
      },
      error: (error: unknown) => this.fail(error),
    });
  }

  private fail(error: unknown): void {
    this.running.set(false);
    this.error.set(isApiError(error) ? error.detail : this.text.failed);
  }
}
