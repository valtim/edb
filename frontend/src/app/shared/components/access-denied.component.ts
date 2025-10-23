import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-access-denied',
    standalone: true,
    imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, RouterModule],
    template: `
    <div class="container">
      <mat-card>
        <mat-card-content>
          <div class="access-denied">
            <mat-icon>block</mat-icon>
            <h1>Acesso Negado</h1>
            <p>Você não tem permissão para acessar esta página.</p>
            <button mat-raised-button color="primary" routerLink="/dashboard">Voltar ao Dashboard</button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
    styles: [`.container { padding: 24px; display: flex; justify-content: center; } .access-denied { text-align: center; } .access-denied mat-icon { font-size: 64px; }`]
})
export class AccessDeniedComponent { }