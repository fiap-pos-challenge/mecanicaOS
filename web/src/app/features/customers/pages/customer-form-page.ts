import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom, map, startWith } from 'rxjs';
import { DraftStore } from '../../../core/drafts/draft.store';
import { applyApiErrors, serverErrorOf } from '../../../core/forms/apply-api-errors';
import { isApiError, isFeaturePending } from '../../../core/http/api-error';
import { messages } from '../../../core/i18n/messages.pt-BR';
import { UiButton } from '../../../shared/ui/button/button';
import { UiCard } from '../../../shared/ui/card/card';
import { UiControl, UiField } from '../../../shared/ui/field/field';
import { ToastService } from '../../../shared/ui/toast/toast';
import { formatDateTime } from '../../../shared/util/date';
import { onlyDigits } from '../../../shared/util/digits';
import { formatDocument } from '../../../shared/util/document';
import { formatPhone } from '../../../shared/util/phone';
import { appValidators } from '../../../shared/util/validators';
import { AddressLookupGateway } from '../data/address-lookup.gateway';
import { Address, Customer, CustomerUpdate } from '../data/customer.models';
import { CustomersGateway } from '../data/customers.gateway';

export const ADDRESS_FIELDS = ['zipCode', 'street', 'number', 'district', 'city', 'state'] as const;

export function formatZipCode(value: string): string {
  const digits = onlyDigits(value).slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

export function completeAddress(group: AbstractControl): ValidationErrors | null {
  const value = group.value as Record<string, string | null>;
  const filled = Object.values(value).some((field) => (field ?? '').trim() !== '');
  if (!filled) {
    return null;
  }

  return ADDRESS_FIELDS.every((field) => (value[field] ?? '').trim() !== '')
    ? null
    : { incompleteAddress: true };
}

function createForm() {
  return new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2), Validators.maxLength(160)],
    }),
    document: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, appValidators.document],
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, appValidators.phone],
    }),
    email: new FormControl('', { nonNullable: true, validators: [appValidators.email] }),
    address: new FormGroup(
      {
        zipCode: new FormControl('', {
          nonNullable: true,
          validators: [Validators.pattern(/^\d{5}-?\d{3}$/)],
        }),
        street: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(150)] }),
        number: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(10)] }),
        complement: new FormControl('', {
          nonNullable: true,
          validators: [Validators.maxLength(60)],
        }),
        district: new FormControl('', {
          nonNullable: true,
          validators: [Validators.maxLength(80)],
        }),
        city: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(80)] }),
        state: new FormControl('', {
          nonNullable: true,
          validators: [Validators.pattern(/^[A-Za-z]{2}$/)],
        }),
      },
      { validators: [completeAddress] },
    ),
    messagingConsent: new FormGroup({
      whatsApp: new FormControl(false, { nonNullable: true }),
      email: new FormControl(false, { nonNullable: true }),
    }),
  });
}

type CustomerForm = ReturnType<typeof createForm>;

@Component({
  selector: 'app-customer-form-page',
  imports: [ReactiveFormsModule, RouterLink, UiButton, UiCard, UiControl, UiField],
  templateUrl: './customer-form-page.html',
  styleUrl: './customer-form-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomerFormPage {
  readonly id = input<string | null | undefined>(null);

  private readonly customers = inject(CustomersGateway);
  private readonly addresses = inject(AddressLookupGateway);
  private readonly drafts = inject(DraftStore);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly text = messages.customers.form;
  protected readonly form: CustomerForm = createForm();
  protected readonly editing = computed(() => Boolean(this.id()));
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly lookingUp = signal(false);
  protected readonly zipMessage = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly draftRestored = signal(false);
  protected readonly consentUpdatedAt = signal<string | null>(null);
  protected readonly cancelLink = computed(() => {
    const id = this.id();
    return id ? ['/customers', id] : ['/customers'];
  });

  private readonly emailFilled = toSignal(
    this.form.controls.email.valueChanges.pipe(
      startWith(this.form.controls.email.value),
      map((email) => email.trim() !== ''),
    ),
    { initialValue: false },
  );
  protected readonly emailConsentBlocked = computed(() => !this.emailFilled());

  constructor() {
    this.form.controls.email.valueChanges.subscribe((email) => {
      const consent = this.form.controls.messagingConsent.controls.email;
      if (email.trim() === '') {
        consent.setValue(false, { emitEvent: false });
        consent.disable({ emitEvent: false });
      } else {
        consent.enable({ emitEvent: false });
      }
    });
    this.form.controls.messagingConsent.controls.email.disable({ emitEvent: false });
    effect(() => {
      const id = this.id();
      untracked(() => void this.prepare(id));
    });
    this.form.valueChanges.subscribe(() =>
      this.drafts.save(this.draftKey(), this.form.getRawValue()),
    );
  }

  protected draftKey(): string {
    return `customer-form:${this.id() || 'new'}`;
  }

  protected errorOf(control: AbstractControl): string | null {
    if (!control.touched || control.valid) {
      return null;
    }

    const server = serverErrorOf(control);
    if (server) {
      return server;
    }

    if (control.hasError('required')) {
      return this.text.required;
    }

    return null;
  }

  protected fieldError(name: 'name' | 'document' | 'phone' | 'email'): string | null {
    const control = this.form.controls[name];
    const base = this.errorOf(control);
    if (base !== null || control.valid || !control.touched) {
      return base;
    }

    return {
      name: this.text.invalidName,
      document: this.text.invalidDocument,
      phone: this.text.invalidPhone,
      email: this.text.invalidEmail,
    }[name];
  }

  protected addressError(name: 'zipCode' | 'state'): string | null {
    const control = this.form.controls.address.controls[name];
    const base = this.errorOf(control);
    if (base !== null || control.valid || !control.touched) {
      return base;
    }

    return name === 'zipCode' ? this.text.invalidZip : this.text.invalidState;
  }

  protected readonly addressIncomplete = () =>
    this.form.controls.address.hasError('incompleteAddress') && this.form.controls.address.touched;

  protected maskDocument(): void {
    const control = this.form.controls.document;
    control.setValue(formatDocument(control.value), { emitEvent: false });
  }

  protected maskPhone(): void {
    const control = this.form.controls.phone;
    control.setValue(formatPhone(control.value), { emitEvent: false });
  }

  protected maskZip(): void {
    const control = this.form.controls.address.controls.zipCode;
    control.setValue(formatZipCode(control.value), { emitEvent: false });
    if (onlyDigits(control.value).length === 8) {
      void this.lookup();
    }
  }

  protected async lookup(): Promise<void> {
    const zip = this.form.controls.address.controls.zipCode;
    if (onlyDigits(zip.value).length !== 8 || this.lookingUp()) {
      zip.markAsTouched();
      return;
    }

    this.lookingUp.set(true);
    this.zipMessage.set(this.text.lookingUp);
    try {
      const address = await firstValueFrom(this.addresses.lookup(zip.value));
      if (address) {
        this.form.controls.address.patchValue({
          street: address.street ?? '',
          district: address.district ?? '',
          city: address.city ?? '',
          state: address.state ?? '',
        });
        this.zipMessage.set(null);
      } else {
        this.zipMessage.set(this.text.zipNotFound);
      }
    } catch (error) {
      this.zipMessage.set(
        isFeaturePending(error) ? messages.errors.featurePending : this.text.zipFailed,
      );
    } finally {
      this.lookingUp.set(false);
    }
  }

  protected async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set(this.text.fixErrors);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const update = this.toUpdate();
    try {
      const id = this.id();
      const saved = id
        ? await firstValueFrom(this.customers.update(id, update))
        : await firstValueFrom(
            this.customers.create({ ...update, document: this.form.controls.document.value }),
          );
      this.drafts.discard(this.draftKey());
      this.toast.show(id ? this.text.updated : this.text.created, { tone: 'success' });
      await this.router.navigate(['/customers', saved.id]);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.saving.set(false);
    }
  }

  private toUpdate(): CustomerUpdate {
    const value = this.form.getRawValue();
    const address = value.address;
    const hasAddress = Object.values(address).some((field) => field.trim() !== '');
    const normalized: Address | null = hasAddress
      ? {
          zipCode: onlyDigits(address.zipCode),
          street: address.street.trim(),
          number: address.number.trim(),
          complement: address.complement.trim() || null,
          district: address.district.trim(),
          city: address.city.trim(),
          state: address.state.trim().toUpperCase(),
        }
      : null;
    return {
      name: value.name.trim(),
      email: value.email.trim() || null,
      phone: value.phone,
      address: normalized,
      messagingConsent: {
        whatsApp: value.messagingConsent.whatsApp,
        email: value.email.trim() !== '' && value.messagingConsent.email,
      },
    };
  }

  private handleError(error: unknown): void {
    if (!isApiError(error)) {
      this.error.set(messages.errors.unknown);
      return;
    }

    if (error.status === 409) {
      this.form.controls.document.setErrors({ server: error.detail });
      this.form.controls.document.markAsTouched();
      this.error.set(error.detail);
      return;
    }

    const unmatched = applyApiErrors(this.form, error);
    this.error.set(
      unmatched.length > 0 || error.fieldErrors.length === 0 ? error.detail : this.text.fixErrors,
    );
  }

  private async prepare(id: string | null | undefined): Promise<void> {
    const draft = this.drafts.restore<ReturnType<CustomerForm['getRawValue']>>(this.draftKey());
    if (id) {
      this.form.controls.document.disable({ emitEvent: false });
      await this.loadCustomer(id, draft === null);
    }

    if (draft) {
      this.form.patchValue(draft);
      this.form.markAsDirty();
      this.draftRestored.set(true);
    }
  }

  private async loadCustomer(id: string, fill: boolean): Promise<void> {
    this.loading.set(true);
    try {
      const customer = await firstValueFrom(this.customers.get(id));
      this.consentUpdatedAt.set(customer.messagingConsent.updatedAt);
      if (fill) {
        await this.fill(customer);
      }
    } catch (error) {
      this.error.set(isApiError(error) ? error.detail : messages.errors.unknown);
    } finally {
      this.loading.set(false);
    }
  }

  private async fill(customer: Customer): Promise<void> {
    const [phone, email] = await Promise.all([
      firstValueFrom(this.customers.reveal(customer.id, 'Phone', 'Edit')),
      customer.maskedEmail
        ? firstValueFrom(this.customers.reveal(customer.id, 'Email', 'Edit'))
        : Promise.resolve(null),
    ]);
    const address = customer.address;
    this.form.patchValue(
      {
        name: customer.name,
        document: customer.maskedDocument,
        phone: formatPhone(phone ?? ''),
        email: email ?? '',
        address: {
          zipCode: formatZipCode(address?.zipCode ?? ''),
          street: address?.street ?? '',
          number: address?.number ?? '',
          complement: address?.complement ?? '',
          district: address?.district ?? '',
          city: address?.city ?? '',
          state: address?.state ?? '',
        },
        messagingConsent: customer.messagingConsent,
      },
      { emitEvent: true },
    );
    this.form.markAsPristine();
  }

  protected consentRecorded(): string | null {
    const date = this.consentUpdatedAt();
    return date ? this.text.consentRecordedAt(formatDateTime(date)) : null;
  }
}
