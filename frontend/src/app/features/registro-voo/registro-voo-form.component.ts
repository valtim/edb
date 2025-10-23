import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatStepperModule } from '@angular/material/stepper';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { RegistroStatus, RegistroVooDto } from '../../core/models';
import { RegistroVooService } from '../../core/services/registro-voo.service';

@Component({
    selector: 'app-registro-voo-form',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        RouterModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatDatepickerModule,
        MatNativeDateModule,
        MatButtonModule,
        MatIconModule,
        MatStepperModule,
        MatProgressSpinnerModule,
        MatSnackBarModule,
        MatTabsModule,
        MatChipsModule
    ],
    template: `
    <div class="form-container">
      <mat-card class="main-card">
        <mat-card-header>
          <mat-card-title>
            <mat-icon>{{isEditing ? 'edit' : 'add'}}</mat-icon>
            {{isEditing ? 'Editar' : 'Novo'}} Registro de Voo
          </mat-card-title>
          <mat-card-subtitle>
            {{isEditing ? 'Modificar dados do registro existente' : 'Criar novo registro conforme ANAC 457/2017'}}
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <mat-horizontal-stepper linear #stepper *ngIf="!isEditing">
            <!-- Etapa 1: Dados Básicos -->
            <mat-step [stepControl]="dadosBasicosForm" label="Dados Básicos">
              <form [formGroup]="dadosBasicosForm" class="step-form">
                <div class="form-row">
                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Aeronave</mat-label>
                    <mat-select formControlName="aeronaveId" required>
                      <mat-option *ngFor="let aeronave of aeronaves" [value]="aeronave.id">
                        {{aeronave.prefixo}} - {{aeronave.modelo}}
                      </mat-option>
                    </mat-select>
                    <mat-error *ngIf="dadosBasicosForm.get('aeronaveId')?.hasError('required')">
                      Selecione uma aeronave
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Número Sequencial</mat-label>
                    <input matInput 
                           type="number" 
                           formControlName="numeroSequencial"
                           readonly>
                    <mat-hint>Gerado automaticamente por aeronave</mat-hint>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Data do Voo</mat-label>
                    <input matInput 
                           [matDatepicker]="datePicker" 
                           formControlName="data"
                           required>
                    <mat-datepicker-toggle matSuffix [for]="datePicker"></mat-datepicker-toggle>
                    <mat-datepicker #datePicker></mat-datepicker>
                    <mat-error *ngIf="dadosBasicosForm.get('data')?.hasError('required')">
                      Data é obrigatória
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Natureza do Voo</mat-label>
                    <mat-select formControlName="naturezaVoo" required>
                      <mat-option value="Instrução">Instrução</mat-option>
                      <mat-option value="Treinamento">Treinamento</mat-option>
                      <mat-option value="Recreativo">Recreativo</mat-option>
                      <mat-option value="Comercial">Comercial</mat-option>
                      <mat-option value="Transporte">Transporte</mat-option>
                      <mat-option value="Teste">Teste</mat-option>
                    </mat-select>
                    <mat-error *ngIf="dadosBasicosForm.get('naturezaVoo')?.hasError('required')">
                      Natureza do voo é obrigatória
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="step-actions">
                  <button mat-raised-button color="primary" matStepperNext>
                    Próximo
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Etapa 2: Tripulação -->
            <mat-step [stepControl]="tripulacaoForm" label="Tripulação">
              <form [formGroup]="tripulacaoForm" class="step-form">
                <div class="form-row">
                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Piloto em Comando</mat-label>
                    <mat-select formControlName="pilotoComandoId" required>
                      <mat-option *ngFor="let piloto of pilotos" [value]="piloto.id">
                        {{piloto.nome}} - ANAC {{piloto.codigoAnac}}
                      </mat-option>
                    </mat-select>
                    <mat-error *ngIf="tripulacaoForm.get('pilotoComandoId')?.hasError('required')">
                      Piloto em comando é obrigatório
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Copiloto (opcional)</mat-label>
                    <mat-select formControlName="copilotoId">
                      <mat-option value="">Nenhum</mat-option>
                      <mat-option *ngFor="let piloto of pilotos" [value]="piloto.id">
                        {{piloto.nome}} - ANAC {{piloto.codigoAnac}}
                      </mat-option>
                    </mat-select>
                  </mat-form-field>
                </div>

                <div class="step-actions">
                  <button mat-stroked-button matStepperPrevious>
                    Anterior
                  </button>
                  <button mat-raised-button color="primary" matStepperNext>
                    Próximo
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Etapa 3: Rota e Horários -->
            <mat-step [stepControl]="rotaForm" label="Rota e Horários">
              <form [formGroup]="rotaForm" class="step-form">
                <div class="form-row">
                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Aeroporto de Partida</mat-label>
                    <input matInput 
                           formControlName="aeroportoPartida"
                           placeholder="SBSP ou São Paulo"
                           maxlength="50"
                           required>
                    <mat-hint>Código IATA, ICAO ou nome</mat-hint>
                    <mat-error *ngIf="rotaForm.get('aeroportoPartida')?.hasError('required')">
                      Aeroporto de partida é obrigatório
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Aeroporto de Destino</mat-label>
                    <input matInput 
                           formControlName="aeroportoDestino"
                           placeholder="SBRJ ou Rio de Janeiro"
                           maxlength="50"
                           required>
                    <mat-hint>Código IATA, ICAO ou nome</mat-hint>
                    <mat-error *ngIf="rotaForm.get('aeroportoDestino')?.hasError('required')">
                      Aeroporto de destino é obrigatório
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Horário de Decolagem (UTC)</mat-label>
                    <input matInput 
                           type="time" 
                           formControlName="horarioDecolagem"
                           required>
                    <mat-error *ngIf="rotaForm.get('horarioDecolagem')?.hasError('required')">
                      Horário de decolagem é obrigatório
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Horário de Pouso (UTC)</mat-label>
                    <input matInput 
                           type="time" 
                           formControlName="horarioPouso"
                           required>
                    <mat-error *ngIf="rotaForm.get('horarioPouso')?.hasError('required')">
                      Horário de pouso é obrigatório
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Tempo de Voo IFR (minutos)</mat-label>
                    <input matInput 
                           type="number" 
                           formControlName="tempoVooIFR"
                           min="0"
                           required>
                    <mat-error *ngIf="rotaForm.get('tempoVooIFR')?.hasError('required')">
                      Tempo IFR é obrigatório
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Pessoas a Bordo</mat-label>
                    <input matInput 
                           type="number" 
                           formControlName="pessoasBordo"
                           min="1"
                           required>
                    <mat-error *ngIf="rotaForm.get('pessoasBordo')?.hasError('required')">
                      Número de pessoas é obrigatório
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="step-actions">
                  <button mat-stroked-button matStepperPrevious>
                    Anterior
                  </button>
                  <button mat-raised-button color="primary" matStepperNext>
                    Próximo
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Etapa 4: Combustível e Carga -->
            <mat-step [stepControl]="combustivelForm" label="Combustível e Carga">
              <form [formGroup]="combustivelForm" class="step-form">
                <div class="form-row">
                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Quantidade de Combustível</mat-label>
                    <input matInput 
                           type="number" 
                           formControlName="combustivelQuantidade"
                           min="0"
                           step="0.1"
                           required>
                    <mat-error *ngIf="combustivelForm.get('combustivelQuantidade')?.hasError('required')">
                      Quantidade de combustível é obrigatória
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="form-field">
                    <mat-label>Unidade</mat-label>
                    <mat-select formControlName="combustivelUnidade" required>
                      <mat-option value="L">Litros (L)</mat-option>
                      <mat-option value="Gal">Galões (Gal)</mat-option>
                      <mat-option value="Kg">Quilogramas (Kg)</mat-option>
                    </mat-select>
                    <mat-error *ngIf="combustivelForm.get('combustivelUnidade')?.hasError('required')">
                      Unidade é obrigatória
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="form-row">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Carga (kg)</mat-label>
                    <input matInput 
                           type="number" 
                           formControlName="carga"
                           min="0"
                           step="0.1"
                           required>
                    <mat-error *ngIf="combustivelForm.get('carga')?.hasError('required')">
                      Peso da carga é obrigatório
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="step-actions">
                  <button mat-stroked-button matStepperPrevious>
                    Anterior
                  </button>
                  <button mat-raised-button color="primary" matStepperNext>
                    Próximo
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Etapa 5: Observações e Finalização -->
            <mat-step [stepControl]="observacoesForm" label="Observações">
              <form [formGroup]="observacoesForm" class="step-form">
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Ocorrências</mat-label>
                  <textarea matInput 
                            formControlName="ocorrencias"
                            rows="3"
                            maxlength="500"
                            placeholder="Descreva qualquer ocorrência durante o voo"></textarea>
                  <mat-hint>Opcional - máximo 500 caracteres</mat-hint>
                </mat-form-field>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Discrepâncias Técnicas</mat-label>
                  <textarea matInput 
                            formControlName="discrepanciasTecnicas"
                            rows="3"
                            maxlength="500"
                            placeholder="Descreva problemas técnicos identificados"></textarea>
                  <mat-hint>Opcional - máximo 500 caracteres</mat-hint>
                </mat-form-field>

                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Ações Corretivas</mat-label>
                  <textarea matInput 
                            formControlName="acoesCorretivas"
                            rows="3"
                            maxlength="500"
                            placeholder="Descreva ações tomadas para corrigir problemas"></textarea>
                  <mat-hint>Opcional - máximo 500 caracteres</mat-hint>
                </mat-form-field>

                <div class="step-actions">
                  <button mat-stroked-button matStepperPrevious>
                    Anterior
                  </button>
                  <button mat-raised-button 
                          color="primary" 
                          (click)="salvarRascunho()"
                          [disabled]="isLoading">
                    <mat-spinner diameter="20" *ngIf="isLoading"></mat-spinner>
                    Salvar como Rascunho
                  </button>
                  <button mat-raised-button 
                          color="primary" 
                          (click)="finalizarRegistro()"
                          [disabled]="isLoading">
                    Finalizar e Assinar
                  </button>
                </div>
              </form>
            </mat-step>
          </mat-horizontal-stepper>

          <!-- Formulário simples para edição -->
          <div *ngIf="isEditing" class="edit-form">
            <mat-tab-group>
              <mat-tab label="Dados Básicos">
                <form [formGroup]="dadosBasicosForm" class="tab-content">
                  <!-- Mesmo conteúdo das etapas, mas em tabs -->
                </form>
              </mat-tab>
              <!-- Mais tabs... -->
            </mat-tab-group>

            <div class="form-actions">
              <button mat-stroked-button [routerLink]="['/registro-voo']">
                Cancelar
              </button>
              <button mat-raised-button 
                      color="primary" 
                      (click)="salvarAlteracoes()"
                      [disabled]="isLoading">
                <mat-spinner diameter="20" *ngIf="isLoading"></mat-spinner>
                Salvar Alterações
              </button>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
    styles: [`
    .form-container {
      max-width: 1200px;
      margin: 0 auto;
    }

    .main-card {
      margin-bottom: 24px;
    }

    .step-form, .tab-content {
      padding: 24px 0;
    }

    .form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
    }

    .form-field {
      flex: 1;
      min-width: 200px;
    }

    .full-width {
      width: 100%;
    }

    .step-actions, .form-actions {
      display: flex;
      gap: 16px;
      justify-content: flex-end;
      margin-top: 24px;
      padding-top: 16px;
      border-top: 1px solid #e0e0e0;
    }

    mat-card-title {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    mat-spinner {
      margin-right: 8px;
    }

    @media (max-width: 768px) {
      .form-row {
        flex-direction: column;
      }
      
      .step-actions, .form-actions {
        flex-direction: column;
      }
    }
  `]
})
export class RegistroVooFormComponent implements OnInit, OnDestroy {
    dadosBasicosForm!: FormGroup;
    tripulacaoForm!: FormGroup;
    rotaForm!: FormGroup;
    combustivelForm!: FormGroup;
    observacoesForm!: FormGroup;

    isEditing = false;
    isLoading = false;
    registroId?: string;

    aeronaves: any[] = [];
    pilotos: any[] = [];

    private destroy$ = new Subject<void>();

    constructor(
        private fb: FormBuilder,
        private registroVooService: RegistroVooService,
        private router: Router,
        private route: ActivatedRoute,
        private snackBar: MatSnackBar
    ) {
        this.createForms();
    }

    ngOnInit(): void {
        this.registroId = this.route.snapshot.params['id'];
        this.isEditing = !!this.registroId;

        this.loadInitialData();

        if (this.isEditing) {
            this.loadRegistro();
        }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    private createForms(): void {
        this.dadosBasicosForm = this.fb.group({
            aeronaveId: ['', Validators.required],
            numeroSequencial: [{ value: '', disabled: true }],
            data: ['', Validators.required],
            naturezaVoo: ['', Validators.required]
        });

        this.tripulacaoForm = this.fb.group({
            pilotoComandoId: ['', Validators.required],
            copilotoId: ['']
        });

        this.rotaForm = this.fb.group({
            aeroportoPartida: ['', Validators.required],
            aeroportoDestino: ['', Validators.required],
            horarioDecolagem: ['', Validators.required],
            horarioPouso: ['', Validators.required],
            tempoVooIFR: ['', [Validators.required, Validators.min(0)]],
            pessoasBordo: ['', [Validators.required, Validators.min(1)]]
        });

        this.combustivelForm = this.fb.group({
            combustivelQuantidade: ['', [Validators.required, Validators.min(0)]],
            combustivelUnidade: ['L', Validators.required],
            carga: ['', [Validators.required, Validators.min(0)]]
        });

        this.observacoesForm = this.fb.group({
            ocorrencias: [''],
            discrepanciasTecnicas: [''],
            acoesCorretivas: ['']
        });

        // Atualizar número sequencial quando aeronave mudar
        this.dadosBasicosForm.get('aeronaveId')?.valueChanges
            .pipe(takeUntil(this.destroy$))
            .subscribe(aeronaveId => {
                if (aeronaveId && !this.isEditing) {
                    this.obterProximoNumeroSequencial(aeronaveId);
                }
            });
    }

    private loadInitialData(): void {
        // Carregar aeronaves e pilotos
        // Implementar serviços para buscar estes dados
    }

    private loadRegistro(): void {
        if (this.registroId) {
            this.registroVooService.obterPorId(this.registroId)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                    next: (registro) => {
                        this.populateForm(registro);
                    },
                    error: (error) => {
                        this.snackBar.open('Erro ao carregar registro', 'Fechar', { duration: 5000 });
                    }
                });
        }
    }

    private populateForm(registro: RegistroVooDto): void {
        // Preencher formulários com dados do registro
        this.dadosBasicosForm.patchValue({
            aeronaveId: registro.aeronaveId,
            numeroSequencial: registro.numeroSequencial,
            data: new Date(registro.data),
            naturezaVoo: registro.naturezaVoo
        });

        // Preencher outros formulários...
    }

    private obterProximoNumeroSequencial(aeronaveId: string): void {
        this.registroVooService.obterProximoNumeroSequencial(aeronaveId)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (numero) => {
                    this.dadosBasicosForm.patchValue({ numeroSequencial: numero });
                },
                error: (error) => {
                    this.snackBar.open('Erro ao obter número sequencial', 'Fechar', { duration: 5000 });
                }
            });
    }

    salvarRascunho(): void {
        if (this.isFormValid()) {
            this.isLoading = true;
            const registro = this.buildRegistroObject();
            registro.status = RegistroStatus.Rascunho;

            this.registroVooService.criar(registro)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                    next: () => {
                        this.isLoading = false;
                        this.snackBar.open('Rascunho salvo com sucesso', 'Fechar', { duration: 3000 });
                        this.router.navigate(['/registro-voo']);
                    },
                    error: (error) => {
                        this.isLoading = false;
                        this.snackBar.open('Erro ao salvar rascunho', 'Fechar', { duration: 5000 });
                    }
                });
        }
    }

    finalizarRegistro(): void {
        if (this.isFormValid()) {
            this.isLoading = true;
            const registro = this.buildRegistroObject();
            registro.status = RegistroStatus.PendenteAssinaturaPiloto;

            this.registroVooService.criar(registro)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                    next: () => {
                        this.isLoading = false;
                        this.snackBar.open('Registro criado e enviado para assinatura', 'Fechar', { duration: 3000 });
                        this.router.navigate(['/registro-voo']);
                    },
                    error: (error) => {
                        this.isLoading = false;
                        this.snackBar.open('Erro ao finalizar registro', 'Fechar', { duration: 5000 });
                    }
                });
        }
    }

    salvarAlteracoes(): void {
        if (this.isFormValid() && this.registroId) {
            this.isLoading = true;
            const registro = this.buildRegistroObject();

            this.registroVooService.atualizar(this.registroId, registro)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                    next: () => {
                        this.isLoading = false;
                        this.snackBar.open('Alterações salvas com sucesso', 'Fechar', { duration: 3000 });
                        this.router.navigate(['/registro-voo']);
                    },
                    error: (error) => {
                        this.isLoading = false;
                        this.snackBar.open('Erro ao salvar alterações', 'Fechar', { duration: 5000 });
                    }
                });
        }
    }

    private isFormValid(): boolean {
        return this.dadosBasicosForm.valid &&
            this.tripulacaoForm.valid &&
            this.rotaForm.valid &&
            this.combustivelForm.valid;
    }

    private buildRegistroObject(): Partial<RegistroVooDto> {
        const dadosBasicos = this.dadosBasicosForm.value;
        const tripulacao = this.tripulacaoForm.value;
        const rota = this.rotaForm.value;
        const combustivel = this.combustivelForm.value;
        const observacoes = this.observacoesForm.value;

        return {
            aeronaveId: dadosBasicos.aeronaveId,
            numeroSequencial: dadosBasicos.numeroSequencial,
            data: dadosBasicos.data.toISOString().split('T')[0],
            naturezaVoo: dadosBasicos.naturezaVoo,
            pilotoComandoId: tripulacao.pilotoComandoId,
            copilotoId: tripulacao.copilotoId || undefined,
            aeroportoPartida: rota.aeroportoPartida,
            aeroportoDestino: rota.aeroportoDestino,
            horarioDecolagem: this.buildDateTime(dadosBasicos.data, rota.horarioDecolagem),
            horarioPouso: this.buildDateTime(dadosBasicos.data, rota.horarioPouso),
            tempoVooIFR: rota.tempoVooIFR,
            pessoasBordo: rota.pessoasBordo,
            combustivelQuantidade: combustivel.combustivelQuantidade,
            combustivelUnidade: combustivel.combustivelUnidade,
            carga: combustivel.carga,
            ocorrencias: observacoes.ocorrencias || undefined,
            discrepanciasTecnicas: observacoes.discrepanciasTecnicas || undefined,
            acoesCorretivas: observacoes.acoesCorretivas || undefined
        };
    }

    private buildDateTime(date: Date, time: string): string {
        const dateStr = date.toISOString().split('T')[0];
        return `${dateStr}T${time}:00.000Z`;
    }
}