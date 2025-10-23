import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'app-not-found',
    standalone: true,
    imports: [CommonModule, MatCardModule, MatButtonModule, MatIconModule, RouterModule],
    template: `
    <div class="container">
      <mat-card>
        <mat-card-content>
          <div class="not-found">
            <mat-icon>error_outline</mat-icon>
            <h1>Página Não Encontrada</h1>
            <p>A página que você está procurando não existe.</p>
            <button mat-raised-button color="primary" routerLink="/dashboard">Voltar ao Dashboard</button>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
    styles: [`.container { padding: 24px; display: flex; justify-content: center; } .not-found { text-align: center; } .not-found mat-icon { font-size: 64px; }`]
})
export class NotFoundComponent { }