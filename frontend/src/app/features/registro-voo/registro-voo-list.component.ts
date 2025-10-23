import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-registro-voo-list',
    standalone: true,
    imports: [CommonModule, RouterModule, MatCardModule, MatButtonModule, MatIconModule],
    template: `
    <div class="container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Registros de Voo</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <p>Lista de registros será implementada aqui</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
    styles: [`.container { padding: 24px; }`]
})
export class RegistroVooListComponent { }